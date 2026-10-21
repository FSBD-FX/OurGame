using System;
using System.IO;
using System.Threading.Tasks;
using Godot;
using Knight.Abilities;
using Knight.Combat;
using Knight.Mechanisms;
using Knight.Player;

namespace Knight.Tests.Presentation;

// Scripted keyboard-event-driven component demonstration, not human/full-prologue play.
public partial class CanyonPressureGateProbe : SceneTree
{
    private static readonly Vector2I WindowSize = new(1280, 720);
    private static readonly Rect2 Floor = new(1280f, 1640f, 600f, 20f);
    private static readonly Rect2 Roof = new(1720f, 1400f, 160f, 24f);
    private static readonly Rect2 RightWall = new(1880f, 1400f, 40f, 280f);
    private CharacterBody2D _player;
    private CanyonPressurePlateGate _gate;
    private Label _stageLabel;
    private bool _aHeld;
    private bool _dHeld;
    private bool _spaceHeld;
    private int _physicsFrames;

    public override async void _Initialize()
    {
        Node2D fixture = null;
        try
        {
            string caseDirectory = ValidateInvocation();
            if (DisplayServer.GetName().Equals("headless", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Task16A component proof requires a normal display window");
            string renderer = ProjectSettings.GetSetting("rendering/renderer/rendering_method").AsString();
            if (renderer != "gl_compatibility")
                throw new InvalidOperationException($"Configured renderer changed: {renderer}");
            Root.ContentScaleSize = WindowSize;
            Root.ContentScaleMode = Window.ContentScaleModeEnum.CanvasItems;
            Root.Size = WindowSize;
            DisplayServer.WindowSetSize(WindowSize);
            fixture = BuildFixture();
            Root.AddChild(fixture);
            await ToSignal(this, SignalName.ProcessFrame);
            if (!fixture.IsNodeReady() || !_player.IsNodeReady() || !_gate.IsNodeReady() ||
                DisplayServer.WindowGetSize() != WindowSize ||
                Root.GetVisibleRect().Size != (Vector2)WindowSize)
                throw new InvalidOperationException("Fixture/window was not ready at 1280x720");
            GD.Print($"TASK16A_PROOF_SETUP kind=scripted-keyboard-event-driven-component " +
                $"not-human-not-full-prologue player={_player.GlobalPosition} " +
                $"camera=(1650,1510) window={DisplayServer.WindowGetSize()} " +
                $"renderer={renderer} adapter={RenderingServer.GetVideoAdapterName()} " +
                $"audio=Dummy movie={Path.Combine(caseDirectory, "raw.avi")}");

            await Demonstrate();
            ReleaseAllKeys();
            GD.Print($"TASK16A_PRESSURE_GATE_PROOF_OK frames={_physicsFrames} " +
                $"player={_player.GlobalPosition} closed={!_gate.IsOpen} " +
                $"adapter={RenderingServer.GetVideoAdapterName()}");
            fixture.QueueFree();
            await ToSignal(this, SignalName.ProcessFrame);
            Quit(0);
        }
        catch (Exception error)
        {
            GD.PushError("TASK16A_PRESSURE_GATE_PROOF_FAIL: " + error);
            ReleaseAllKeys();
            if (GodotObject.IsInstanceValid(fixture) && fixture.IsInsideTree())
            {
                fixture.QueueFree();
                await ToSignal(this, SignalName.ProcessFrame);
            }
            Quit(1);
        }
    }

    private async Task Demonstrate()
    {
        Stage("Initial regional setup: real player left of closed gate");
        await Frames(45);
        Require(_player.IsOnFloor() && _player.GlobalPosition.X < 1600f &&
            !_gate.IsOpen && !_gate.DoorCollision.Disabled &&
            !_gate.Frame.Traversable && !_gate.HasOpened,
            "initial setup must be grounded and physically closed without false activation");

        Stage("D: step on the real pressure plate");
        SendKey(Key.D, true);
        await Until("grounded plate opens committed door", 100, () =>
            _player.IsOnFloor() && _player.GlobalPosition.X >= 1640f &&
            _player.GlobalPosition.X < 1710f && _gate.IsOpen &&
            _gate.DoorCollision.Disabled && _gate.Frame.Traversable && _gate.HasOpened);

        Stage("D: cross the actual open doorway");
        await Until("player crosses into alcove", 100, () =>
            _player.GlobalPosition.X >= 1790f && _player.GlobalPosition.X < 1850f &&
            _gate.IsOpen && _gate.DoorCollision.Disabled && _gate.Frame.Traversable);
        SendKey(Key.D, false);

        Stage("Interior hold: door stays open past its full closing deadline");
        await Frames(140);
        Require(_player.IsOnFloor() && _player.GlobalPosition.X >= 1775f &&
            _player.GlobalPosition.X < 1860f && _gate.IsOpen &&
            !_gate.IsDangerTelegraphActive && _gate.Frame.Traversable,
            "stationary real interior player must hold the committed door open");

        Stage("Space: normal Player-controller jump and landing inside");
        int jumpsBefore = _player.GetNode<PlayerMotor>("Motor").AcceptedJumpCount;
        float groundY = _player.GlobalPosition.Y;
        SendKey(Key.Space, true);
        await Until("accepted jump becomes airborne", 24, () =>
            _player.GetNode<PlayerMotor>("Motor").AcceptedJumpCount > jumpsBefore &&
            !_player.IsOnFloor() && _player.GlobalPosition.Y < groundY - 8f);
        await Frames(8);
        SendKey(Key.Space, false);
        await Until("same jump lands inside", 100, () =>
            _player.IsOnFloor() && Mathf.Abs(_player.GlobalPosition.Y - groundY) < 5f &&
            _gate.IsOpen && _gate.Frame.Traversable);

        Stage("A: return through the still-open door to safe left floor");
        SendKey(Key.A, true);
        await Until("player returns left across door and plate", 130, () =>
            _player.GlobalPosition.X <= 1545f && _player.IsOnFloor() && _gate.IsOpen);
        SendKey(Key.A, false);
        await Frames(16);
        Require(_player.GlobalPosition.X < 1600f && _player.IsOnFloor(),
            "released left input leaves the real player safely outside the plate");

        Stage("Release: full 1.40 s grace, then harmless 0.55 s warning");
        await Until("harmless closing warning begins", 150, () =>
            _gate.IsDangerTelegraphActive && _gate.IsOpen &&
            _gate.Frame.Traversable && !_gate.Frame.HazardActive);
        Stage("Orange countdown: still traversable, never damaging");
        int warningFrames = 0;
        while (_gate.IsOpen && warningFrames < 60)
        {
            Require(_gate.IsDangerTelegraphActive && _gate.Frame.Traversable &&
                !_gate.Frame.HazardActive,
                "warning must remain visible, traversable and harmless until close");
            warningFrames++;
            await NextPhysics();
            EnsureLive();
        }
        Require(warningFrames >= 25 && !_gate.IsOpen &&
            !_gate.DoorCollision.Disabled && !_gate.Frame.Traversable &&
            _player.GlobalPosition.X < 1600f,
            $"full warning must end in a real safe closed collision; warningFrames={warningFrames}");
        GD.Print($"TASK16A_PROOF_OBSERVED predicate=full-warning-and-physical-close " +
            $"frame={_physicsFrames} warningFrames={warningFrames} " +
            $"closed={!_gate.IsOpen} shapeDisabled={_gate.DoorCollision.Disabled}");
        Stage("Closed: optional alcove completed; main route not exercised");
        await Frames(45);
    }

    private Node2D BuildFixture()
    {
        var root = new Node2D { Name = "Task16AArchiveComponentOnly" };
        root.AddChild(new FixturePaint { Name = "MatchingCollisionPaint" });
        root.AddChild(Solid("ArchiveFloor", Floor));
        root.AddChild(Solid("AlcoveRoof", Roof));
        root.AddChild(Solid("RightWall", RightWall));

        _player = GD.Load<PackedScene>("res://scenes/player/Player.tscn")
            .Instantiate<CharacterBody2D>();
        _player.Name = "ActualPlayer";
        _player.GlobalPosition = new Vector2(1530f, 1604f); // Labeled initial setup only.
        _player.GetNode<Camera2D>("Camera2D").Enabled = false;
        root.AddChild(_player);

        _gate = new CanyonPressurePlateGate { Name = "ArchiveGate", PlayerBody = _player };
        var door = Solid("Door", new Rect2(1720f, 1424f, 40f, 216f));
        door.AddChild(new GrappleSurface2D { Name = "GrappleSurfaceProfile",
            Material = GrappleSurfaceMaterial.Forbidden, UseCollisionHitPoint = true });
        _gate.AddChild(door);
        _gate.AddChild(new CanyonPressureGateVisual { Name = "RemovablePressurePaint" });
        root.AddChild(_gate);

        root.AddChild(new Camera2D
        {
            Name = "FixedComponentCamera", Enabled = true,
            Position = new Vector2(1650f, 1510f), Zoom = new Vector2(1.3f, 1.3f)
        });
        var hud = new CanvasLayer { Name = "ComponentProofLabels" };
        var bar = new ColorRect { Color = new Color(0.035f, 0.055f, 0.08f, .88f),
            Position = Vector2.Zero, Size = new Vector2(1280f, 96f), MouseFilter = Control.MouseFilterEnum.Ignore };
        hud.AddChild(bar);
        hud.AddChild(TextLabel("SCRIPTED COMPONENT VALIDATION · INITIAL ARCHIVE SETUP · NOT FULL PROLOGUE",
            new Vector2(22f, 9f), 21, new Color("f7e8c4")));
        _stageLabel = TextLabel("Setup", new Vector2(22f, 43f), 19, new Color("ffc979"));
        hud.AddChild(_stageLabel);
        hud.AddChild(TextLabel("BRASS PLATE → DOOR · ORANGE TICKS = HARMLESS CLOSING COUNTDOWN",
            new Vector2(22f, 73f), 14, new Color("c5d3db")));
        root.AddChild(hud);
        return root;
    }

    private static StaticBody2D Solid(string name, Rect2 world)
    {
        var body = new StaticBody2D { Name = name, GlobalPosition = world.GetCenter(),
            CollisionLayer = CombatCollisionLayers.World };
        body.AddChild(new CollisionShape2D { Name = "Collision",
            Shape = new RectangleShape2D { Size = world.Size } });
        return body;
    }

    private static Label TextLabel(string text, Vector2 position, int size, Color color)
    {
        var label = new Label { Text = text, Position = position,
            MouseFilter = Control.MouseFilterEnum.Ignore };
        label.AddThemeFontSizeOverride("font_size", size);
        label.AddThemeColorOverride("font_color", color);
        label.AddThemeColorOverride("font_shadow_color", Colors.Black);
        return label;
    }

    private sealed partial class FixturePaint : Node2D
    {
        public override void _Draw()
        {
            DrawRect(new Rect2(1080f, 1130f, 1280f, 740f), new Color("142433"), true);
            DrawRect(Floor, new Color("52616a"), true);
            DrawRect(Roof, new Color("586b72"), true);
            DrawRect(RightWall, new Color("586b72"), true);
            DrawLine(Floor.Position, new Vector2(Floor.End.X, Floor.Position.Y),
                new Color("dfc998"), 3f);
            DrawRect(Roof, new Color("c8ae81"), false, 2f);
            DrawRect(RightWall, new Color("c8ae81"), false, 2f);
        }
    }

    private async Task Until(string predicate, int maxFrames, Func<bool> satisfied)
    {
        for (int index = 0; index < maxFrames; index++)
        {
            EnsureLive();
            if (satisfied())
            {
                GD.Print($"TASK16A_PROOF_OBSERVED predicate={predicate} frame={_physicsFrames} " +
                    $"player={_player.GlobalPosition} grounded={_player.IsOnFloor()} " +
                    $"gateOpen={_gate.IsOpen} traversable={_gate.Frame.Traversable} " +
                    $"warning={_gate.IsDangerTelegraphActive}");
                return;
            }
            await NextPhysics();
        }
        throw new InvalidOperationException($"Timeout ({maxFrames} physics frames): {predicate}; " +
            $"player={_player.GlobalPosition} grounded={_player.IsOnFloor()} " +
            $"gateOpen={_gate.IsOpen} traversable={_gate.Frame.Traversable}");
    }

    private async Task Frames(int count)
    {
        for (int index = 0; index < count; index++)
        {
            await NextPhysics();
            EnsureLive();
        }
    }

    private async Task NextPhysics()
    {
        await ToSignal(this, SignalName.PhysicsFrame);
        _physicsFrames++;
    }

    private void EnsureLive()
    {
        Require(GodotObject.IsInstanceValid(_player) && _player.IsInsideTree() &&
            GodotObject.IsInstanceValid(_gate) && _gate.IsInsideTree() &&
            _player.GetNode<HealthComponent>("Health").CurrentHealth > 0 &&
            float.IsFinite(_player.GlobalPosition.X) && float.IsFinite(_player.GlobalPosition.Y),
            "real player/gate must remain alive, mounted, and at finite position");
    }

    private void Stage(string text)
    {
        _stageLabel.Text = text;
        GD.Print($"TASK16A_PROOF_STAGE frame={_physicsFrames} stage={text} " +
            $"player={_player.GlobalPosition} gateOpen={_gate.IsOpen} " +
            $"warning={_gate.IsDangerTelegraphActive}");
    }

    private void SendKey(Key key, bool pressed)
    {
        using var input = new InputEventKey
        {
            Keycode = key, PhysicalKeycode = key, Pressed = pressed, Echo = false
        };
        Input.ParseInputEvent(input);
        Input.FlushBufferedEvents();
        if (key == Key.A) _aHeld = pressed;
        if (key == Key.D) _dHeld = pressed;
        if (key == Key.Space) _spaceHeld = pressed;
        GD.Print($"TASK16A_PROOF_INPUT frame={_physicsFrames} " +
            $"key={key} pressed={pressed} via=InputEventKey+InputMap");
    }

    private void ReleaseAllKeys()
    {
        if (_aHeld) SendKey(Key.A, false);
        if (_dHeld) SendKey(Key.D, false);
        if (_spaceHeld) SendKey(Key.Space, false);
    }

    public override void _Finalize() => ReleaseAllKeys();

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static string ValidateInvocation()
    {
        string[] args = OS.GetCmdlineUserArgs();
        if (args.Length != 2 || args[0] != "--component-proof" ||
            !args[1].StartsWith("--case-storage-root=", StringComparison.Ordinal))
            throw new ArgumentException("Task16A proof accepts only its dedicated case storage argument");
        string raw = args[1]["--case-storage-root=".Length..];
        if (!Path.IsPathFullyQualified(raw)) throw new ArgumentException("Storage must be absolute");
        string storage = Path.GetFullPath(raw).TrimEnd(Path.DirectorySeparatorChar);
        string caseDirectory = Path.GetDirectoryName(storage) ?? "";
        string evidence = Path.GetFullPath(
            "E:/OurGame/Knight/.evidence/september09-continuous-canyon/task16a")
            .TrimEnd(Path.DirectorySeparatorChar);
        string project = Path.GetFullPath(ProjectSettings.GlobalizePath("res://"))
            .TrimEnd(Path.DirectorySeparatorChar);
        string expectedProject = Path.GetFullPath(
            "E:/OurGame/Knight/.worktrees/task6-certified-final-20260902T0555")
            .TrimEnd(Path.DirectorySeparatorChar);
        string temp = Path.Combine(caseDirectory, "unit-temp");
        if (!Directory.Exists(storage) || !Directory.Exists(temp) ||
            !string.Equals(Path.GetFileName(storage), "storage", StringComparison.Ordinal) ||
            !string.Equals(Path.GetDirectoryName(caseDirectory), evidence,
                StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(project, expectedProject, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar),
                temp, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Proof storage, project, or child temp escaped dedicated case");
        foreach (string variable in new[] { "TMP", "TEMP" })
        {
            string value = System.Environment.GetEnvironmentVariable(variable) ?? "";
            if (!Path.IsPathFullyQualified(value) ||
                !string.Equals(Path.GetFullPath(value).TrimEnd(Path.DirectorySeparatorChar),
                    temp, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException($"Child {variable} escaped dedicated case");
        }
        foreach (string path in new[] { storage, temp })
            for (DirectoryInfo directory = new(path); directory != null; directory = directory.Parent)
                if (directory.Exists && (directory.Attributes & FileAttributes.ReparsePoint) != 0)
                    throw new ArgumentException($"Reparse ancestor rejected: {directory.FullName}");
        return caseDirectory;
    }
}
