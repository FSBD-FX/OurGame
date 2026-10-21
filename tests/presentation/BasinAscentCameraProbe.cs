using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Knight.Combat;
using Knight.Player;
using Knight.Prologue;
using Knight.Prologue.Seamless;
using Knight.Shared;
using Knight.Tests.Integration;

namespace Knight.Tests.Presentation;

// Catches sector-only camera limits clipping the live player's body at the Basin/Ascent seam.
// The sole placement is the authored entry spawn before adding main to the tree. All later
// travel uses InputMap, with normal AI, damage, camera scheduling and physics left enabled.
public partial class BasinAscentCameraProbe : SceneTree
{
    private PrologueLevelController _level;
    private CharacterBody2D _player;
    private CollisionShape2D _body;
    private Camera2D _camera;
    private HealthComponent _health;
    private string _output, _isolatedDirectory, _isolatedId;
    private string _stage = "setup";
    private bool _measure;
    private float _requiredMargin;
    private int _recoveries, _deaths;
    private bool _capturedClip;
    private readonly Dictionary<string, Samples> _samples = new();

    private sealed class Samples
    {
        public int Count, Clipped, BelowRequired;
        public float Minimum = float.PositiveInfinity;
        public float Required;
    }

    public override async void _Initialize()
    {
        int result = 1;
        try
        {
            await ToSignal(this, SignalName.ProcessFrame);
            if (DisplayServer.GetName() == "headless")
                throw new InvalidOperationException("Basin camera probe requires a rendered display.");
            ConfigureOutput();
            ReleaseInputs();
            _level = GD.Load<PackedScene>("res://scenes/prologue/PrologueLevel.tscn")
                .Instantiate<PrologueLevelController>();
            _player = _level.GetNode<CharacterBody2D>("PlayerRoot/Player");
            _player.Position = new Vector2(2380, 1568);
            _body = _player.GetNode<CollisionShape2D>("BodyCollider");
            _camera = _player.GetNode<Camera2D>("Camera2D");
            _health = _player.GetNode<HealthComponent>("Health");
            TestAssert.True(_body.Shape is CapsuleShape2D, "measure the actual player body capsule");
            var flow = _level.GetNode<PrologueGameFlowController>("GameFlow");
            _isolatedId = Guid.NewGuid().ToString("N");
            string isolated = "user://basin-ascent-camera-probe/" + _isolatedId;
            _isolatedDirectory = Path.GetFullPath(ProjectSettings.GlobalizePath(isolated));
            flow.AutomationAutoStart = true;
            flow.SavePath = isolated + "/save.json";
            flow.SettingsPath = isolated + "/settings.json";
            _health.Died += OnDied;
            _level.RecoveryRequested += OnRecovery;
            Root.AddChild(_level);
            await Frames(8);
            TestAssert.Equal(PrologueSectorId.CanyonBasin, _level.CurrentSector.Value, "authored entry mounts Basin");
            StaticBody2D entry = _level.CurrentSectorRoot.GetNode<StaticBody2D>("Gameplay/Collision/EntryCatch");
            StaticBody2D upper = _level.CurrentSectorRoot.GetNode<StaticBody2D>("Gameplay/Collision/SalvageFloor");
            StaticBody2D floor = _level.CurrentSectorRoot.GetNode<StaticBody2D>("Gameplay/Collision/BasinFloor");
            Phase("entry", false);
            await Settle(entry, 90);
            Capture("entry");
            Phase("upper-travel", false);
            Send(InputActions.MoveRight, true);
            await Until(() => _player.GlobalPosition.X >= 2585, 180, "D off EntryCatch");
            Send(InputActions.MoveRight, false);
            await Settle(upper, 180);
            Capture("upper-arrival");

            // Moving samples require no clipping; the three explicit settled holds require
            // 16 logical viewport pixels on BOTH sides of the projected body bounds.
            Phase("basin-edge-descent", true);
            Send(InputActions.MoveRight, true);
            await Until(() => _player.GlobalPosition.X >= 2855, 180, "D off upper right edge");
            Send(InputActions.MoveRight, false);
            await Settle(floor, 120);
            TestAssert.Equal(PrologueSectorId.CanyonBasin, _level.CurrentSector.Value, "right-edge stop remains in Basin");
            Phase("basin-edge-hold", true, 16f);
            await Frames(30);
            TestAssert.Equal(PrologueSectorId.CanyonBasin, _level.CurrentSector.Value, "edge hold stays in Basin with live combat");
            Capture("basin-edge-hold");

            Phase("cross-to-ascent", true);
            Send(InputActions.MoveRight, true);
            await Until(() => _player.GlobalPosition.X >= 3010, 180, "D across Basin/Ascent seam");
            Send(InputActions.MoveRight, false);
            TestAssert.Equal(PrologueSectorId.CanyonAscent, _level.CurrentSector.Value, "real D crossing enters Ascent");
            StaticBody2D ascentFloor = _level.CurrentSectorRoot.GetNode<StaticBody2D>("Gameplay/Collision/AscentEntry");
            await Settle(ascentFloor, 90);
            Phase("ascent-hold", true, 16f);
            await Frames(24);
            TestAssert.Equal(PrologueSectorId.CanyonAscent, _level.CurrentSector.Value, "forward hold stays in Ascent");
            Capture("ascent-hold");

            Phase("cross-to-basin", true);
            Send(InputActions.MoveLeft, true);
            await Until(() => _player.GlobalPosition.X <= 2850, 180, "A back across the same seam");
            Send(InputActions.MoveLeft, false);
            TestAssert.Equal(PrologueSectorId.CanyonBasin, _level.CurrentSector.Value, "real A crossing returns to Basin");
            await Settle(floor, 90);
            Phase("basin-return-hold", true, 16f);
            await Frames(24);
            TestAssert.Equal(PrologueSectorId.CanyonBasin, _level.CurrentSector.Value, "return hold stays in Basin");
            Capture("basin-return-hold");

            CheckAlive();
            TestAssert.Equal(0, _player.GetNode<PlayerMotor>("Motor").AcceptedJumpCount, "camera route uses no jump");
            TestAssert.Equal(0, _player.GetNode<PlayerMotor>("Motor").AcceptedDashCount, "camera route uses no dash");
            foreach (string hold in new[] { "basin-edge-hold", "ascent-hold", "basin-return-hold" })
                TestAssert.True(_samples.TryGetValue(hold, out Samples observed) && observed.Count > 0,
                    "hold must have real FramePostDraw samples: " + hold);
            // Accumulate camera failures until the entire real route has completed, so RED
            // movies show both crossings rather than ending at the first clipped frame.
            TestAssert.True(_samples.Values.All(s => s.Clipped == 0 && s.BelowRequired == 0),
                "camera seam: moving body must not clip; settled holds need >=16px horizontal body margin");
            GD.Print("BASIN_ASCENT_CAMERA_OK real_input=True moving_min=0 hold_min=16 recovery=0 death=0");
            result = 0;
        }
        catch (Exception exception)
        {
            GD.PushError("BASIN_ASCENT_CAMERA_FAILED: " + exception);
            try
            {
                if (GodotObject.IsInstanceValid(_player) && _player.IsInsideTree()) Capture("route-failed");
            }
            catch (Exception captureException) { GD.PushError("BASIN_CAMERA_CAPTURE_FAILED: " + captureException); }
        }
        finally
        {
            foreach (var pair in _samples)
                GD.Print($"BASIN_CAMERA_SUMMARY stage={pair.Key} samples={pair.Value.Count} min_margin={pair.Value.Minimum} clipped={pair.Value.Clipped} required={pair.Value.Required} below_required={pair.Value.BelowRequired}");
            ReleaseInputs();
            if (GodotObject.IsInstanceValid(_health)) _health.Died -= OnDied;
            try
            {
                try
                {
                    if (GodotObject.IsInstanceValid(_level))
                    {
                        _level.RecoveryRequested -= OnRecovery;
                        if (_level.IsInsideTree()) await TestSceneTeardown.FreeSceneAfterAudioDrain(this, _level);
                        else _level.Free();
                    }
                }
                finally { RemoveOwnedSession(); }
            }
            catch (Exception exception)
            {
                result = 1;
                GD.PushError("BASIN_ASCENT_CAMERA_CLEANUP_FAILED: " + exception);
            }
            Quit(result);
        }
    }

    private void Phase(string stage, bool measure, float required = 0f)
    {
        _stage = stage;
        _measure = measure;
        _requiredMargin = required;
        if (measure) _samples.Add(stage, new Samples { Required = required });
    }

    private void ObservePostDraw()
    {
        CheckAlive();
        if (!_measure) return;
        var capsule = (CapsuleShape2D)_body.Shape;
        Vector2 half = new(capsule.Radius, capsule.Height * .5f);
        Vector2[] local = [new(-half.X, -half.Y), new(half.X, -half.Y), half, new(-half.X, half.Y)];
        Vector2[] world = local.Select(p => _body.GlobalTransform * p).ToArray();
        // Both the capsule corners and viewport bounds are in logical viewport coordinates:
        // do not mix window stretch/GetFinalTransform into this comparison.
        Viewport viewport = _player.GetViewport();
        Transform2D canvas = viewport.GetCanvasTransform();
        Vector2[] projected = world.Select(p => canvas * p).ToArray();
        Rect2 screen = viewport.GetVisibleRect();
        float left = projected.Min(p => p.X) - screen.Position.X;
        float right = screen.End.X - projected.Max(p => p.X);
        float margin = Mathf.Min(left, right);
        TestAssert.True(float.IsFinite(margin), "body projection must be finite");
        Samples sample = _samples[_stage];
        sample.Count++;
        sample.Minimum = Mathf.Min(sample.Minimum, margin);
        if (margin < 0f) sample.Clipped++;
        if (margin < _requiredMargin) sample.BelowRequired++;
        GD.Print($"BASIN_CAMERA_SAMPLE stage={_stage} physics={Engine.GetPhysicsFrames()} sample={sample.Count} sector={_level.CurrentSector} player={_player.GlobalPosition} velocity={_player.Velocity} hp={_health.CurrentHealth} limits=({_camera.LimitLeft},{_camera.LimitTop},{_camera.LimitRight},{_camera.LimitBottom}) zoom={_camera.Zoom} offset={_camera.Offset} canvas={canvas} viewport={screen} world_corners={string.Join(';', world)} projected_corners={string.Join(';', projected)} left={left} right={right} required={_requiredMargin}");
        if (!_capturedClip && margin < 0f)
        {
            _capturedClip = true;
            Capture("first-clipped");
        }
    }

    private void CheckAlive()
    {
        TestAssert.True(_recoveries == 0 && _deaths == 0 && _health.CurrentHealth > 0,
            "natural death/recovery invalidates the continuous camera route");
    }
    private void OnDied() => _deaths++;
    private void OnRecovery(string reason) => _recoveries++;

    private async Task Frames(int count)
    {
        ulong end = Engine.GetPhysicsFrames() + (ulong)count;
        while (Engine.GetPhysicsFrames() < end)
        {
            await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            ObservePostDraw(); // Every actual draw is measured, including repeated physics ticks.
        }
    }
    private async Task Until(Func<bool> condition, int budget, string description)
    {
        ulong end = Engine.GetPhysicsFrames() + (ulong)budget;
        while (!condition() && Engine.GetPhysicsFrames() < end) await Frames(1);
        GD.Print($"BASIN_CAMERA_ROUTE stage={_stage} action={description} player={_player.GlobalPosition} sector={_level.CurrentSector}");
        TestAssert.True(condition(), "route timeout: " + description);
    }
    private async Task Settle(StaticBody2D support, int budget)
    {
        int stable = 0;
        ulong previous = 0;
        await Until(() =>
        {
            ulong tick = Engine.GetPhysicsFrames();
            if (tick == previous) return stable >= 12;
            previous = tick;
            bool supported = _player.IsOnFloor() && Mathf.Abs(_player.Velocity.X) < 1f &&
                Enumerable.Range(0, _player.GetSlideCollisionCount()).Any(i =>
                {
                    var collision = _player.GetSlideCollision(i);
                    return collision.GetCollider() == support && collision.GetNormal().Y < -.7f;
                });
            stable = supported ? stable + 1 : 0;
            return stable >= 12;
        }, budget, "12 distinct stable physics samples on " + support.Name);
    }
    private void Capture(string stage)
    {
        if (_output == null) return;
        using Image frame = Root.GetTexture().GetImage();
        TestAssert.Equal(Error.Ok, frame.SavePng(Path.Combine(_output, stage + ".png")), "save camera evidence");
    }
    private void ConfigureOutput()
    {
        _output = Array.Find(OS.GetCmdlineUserArgs(), a => a.StartsWith("--output="))?[9..];
        if (_output == null) return;
        if (!Path.IsPathFullyQualified(_output)) throw new ArgumentException("Absolute output required.");
        _output = Path.GetFullPath(_output);
        string project = Path.GetFullPath(ProjectSettings.GlobalizePath("res://")).TrimEnd('/', '\\') + Path.DirectorySeparatorChar;
        if ((_output.TrimEnd('/', '\\') + Path.DirectorySeparatorChar).StartsWith(project, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Evidence must be outside the project.");
        Directory.CreateDirectory(_output);
    }
    private void RemoveOwnedSession()
    {
        if (_isolatedDirectory == null || !Directory.Exists(_isolatedDirectory)) return;
        string parent = Path.GetFullPath(ProjectSettings.GlobalizePath("user://basin-ascent-camera-probe"));
        string exact = Path.GetFullPath(Path.Combine(parent, _isolatedId));
        if (!Guid.TryParseExact(_isolatedId, "N", out _) ||
            !string.Equals(exact, _isolatedDirectory, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(Path.GetDirectoryName(exact), parent, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Refuse cleanup outside this probe's GUID session.");
        Directory.Delete(exact, true);
    }
    private static void Send(StringName action, bool pressed)
    {
        Input.ParseInputEvent(new InputEventAction { Action = action, Pressed = pressed, Strength = pressed ? 1 : 0 });
        Input.FlushBufferedEvents();
    }
    private static void ReleaseInputs()
    {
        foreach (StringName action in new[] { InputActions.MoveLeft, InputActions.MoveRight, InputActions.MoveUp,
            InputActions.MoveDown, InputActions.Jump, InputActions.Dash, InputActions.PrimaryAction,
            InputActions.Shield, InputActions.SelectGrappleMode, InputActions.SelectAttackMode }) Send(action, false);
    }
}
