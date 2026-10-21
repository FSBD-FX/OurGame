using System;
using System.IO;
using System.Threading.Tasks;
using Godot;
using Knight.Abilities;
using Knight.Player;
using Knight.Prologue;
using Knight.Prologue.Mechanisms;
using Knight.Prologue.Seamless;
using Knight.Settings;
using Knight.Shared;
using Knight.Tests.Integration;

namespace Knight.Tests.Presentation;

// The normal-speed/capture route starts at the authored Basin checkpoint. All subsequent
// movement is real InputMap input; no injected grapple state, motor step, position or velocity.
public partial class BasinCargoProbe : SceneTree
{
    private CharacterBody2D _player;
    private MechanicalClawController _claw;
    private Vector2 _aim = new(2040, 1806); // old decoy, 20.88px from the real BasinGripStone
    private string _output;
    private int _recoveries, _latches, _releases, _boosts;
    private MechanicalClawState _lastState;

    public override async void _Initialize()
    {
        PrologueLevelController level = null;
        int result = 1;
        try
        {
            await ToSignal(this, SignalName.ProcessFrame);
            if (DisplayServer.GetName() == "headless") throw new InvalidOperationException("Display required.");
            if (Array.Exists(OS.GetCmdlineUserArgs(), a => a == "--exact-anchor")) _aim = new Vector2(2060, 1800);
            _output = Array.Find(OS.GetCmdlineUserArgs(), a => a.StartsWith("--output="))?[9..];
            if (_output != null)
            {
                if (!Path.IsPathFullyQualified(_output)) throw new ArgumentException("Absolute output required.");
                string project = Path.GetFullPath(ProjectSettings.GlobalizePath("res://")).TrimEnd('/', '\\') + Path.DirectorySeparatorChar;
                _output = Path.GetFullPath(_output);
                if ((_output.TrimEnd('/', '\\') + Path.DirectorySeparatorChar).StartsWith(project, StringComparison.OrdinalIgnoreCase))
                    throw new ArgumentException("Evidence must stay outside the project.");
                Directory.CreateDirectory(_output);
            }
            Release();
            level = GD.Load<PackedScene>("res://scenes/prologue/PrologueLevel.tscn").Instantiate<PrologueLevelController>();
            _player = level.GetNode<CharacterBody2D>("PlayerRoot/Player");
            _player.Position = new Vector2(2380, 2014); // sole pre-tree spawn at Basin checkpoint
            _claw = _player.GetNode<MechanicalClawController>("MechanicalClaw");
            string isolated = $"user://basin-cargo-probe/{Guid.NewGuid():N}";
            var flow = level.GetNode<PrologueGameFlowController>("GameFlow");
            flow.AutomationAutoStart = true;
            flow.SavePath = isolated + "/save.json";
            flow.SettingsPath = isolated + "/settings.json";
            level.RecoveryRequested += OnRecovery;
            _claw.ClawFrameChanged += OnClaw;
            Root.AddChild(level);
            await Frames(8);
            if (Array.Exists(OS.GetCmdlineUserArgs(), a => a == "--english"))
            {
                GameSettingsData settings = flow.Settings.Normalize();
                settings.Language = GameLanguage.English;
                flow.ApplySettings(settings);
            }
            TestAssert.Equal(PrologueSectorId.CanyonBasin, level.CurrentSector.Value, "authored Basin checkpoint fixture");
            level.GetNode<PrologueCompletionController>("Completion").RestoreSession(PrologueObjective.LearnShield);
            StaticBody2D floor = level.CurrentSectorRoot.GetNode<StaticBody2D>("Gameplay/Collision/BasinFloor");
            await Settle(floor, 60);
            Send(InputActions.SelectGrappleMode, true);
            await Frames(2);
            Send(InputActions.SelectGrappleMode, false);
            await Capture("aim");
            var resolution = new MechanicalClawTargetResolver().Resolve(_player, _aim);
            GD.Print($"BASIN_CARGO_AIM target={resolution.Surface?.GetPath()} anchor={resolution.Surface?.WorldAnchor} legacy={resolution.Snapshot.IsLegacyInteraction} visible={resolution.Snapshot.HasLineOfSight} range={_player.GlobalPosition.DistanceTo(_aim)}");
            Send(InputActions.PrimaryAction, true);
            await Frames(35);
            await Capture("held");
            float ropeBefore = _claw.CurrentFrame.RopeLength;
            Send(InputActions.MoveUp, true);
            await Frames(20);
            Send(InputActions.MoveUp, false);
            await Capture("reel");
            CanyonClawMechanism2D cargo = level.CurrentSectorRoot.GetNodeOrNull<CanyonClawMechanism2D>("Gameplay/Mechanisms/CargoPlatform");
            GD.Print($"BASIN_CARGO_RESULT position={_player.GlobalPosition} state={_claw.CurrentFrame.State} rope_before={ropeBefore} rope_after={_claw.CurrentFrame.RopeLength} activated={cargo?.IsActivated} linked={cargo?.GetNode<Node2D>("LinkedNode").Position} latches={_latches} releases={_releases}");
            bool held = Attached(_claw.CurrentFrame.State) && _latches == 1 && _releases == 0 &&
                _claw.CurrentTarget?.Name == "BasinGripStone" && _claw.CurrentFrame.RopeLength < ropeBefore - 40;
            Send(InputActions.PrimaryAction, false);
            await Frames(2);
            Send(InputActions.MoveRight, true);
            await Until(() => _player.GlobalPosition.X >= 2380, 180, "return right to the existing mainline");
            Send(InputActions.MoveRight, false);
            await Settle(floor, 180);
            await Capture("return");
            TestAssert.Equal(0, _recoveries, "no recovery or teleport on this detour");
            TestAssert.Equal(0, _player.GetNode<PlayerMotor>("Motor").AcceptedJumpCount, "no ordinary jump required");
            TestAssert.Equal(0, _player.GetNode<PlayerMotor>("Motor").AcceptedDashCount, "no dash required");
            TestAssert.True(held, "near the usable stone, held LMB and W must keep one real attachment and shorten the rope; no decoy auto-release");
            TestAssert.Equal(1, _releases, "the held attachment releases only when the probe releases LMB");
            TestAssert.Equal(0, _boosts, "this short targeting check does not need an assist boost");
            TestAssert.Equal(MechanicalClawState.Idle, _claw.CurrentFrame.State, "the rope has retracted on return");
            GD.Print($"BASIN_CARGO_OK latches={_latches} releases={_releases} boosts={_boosts} recovery=0 jump=0 dash=0");
            result = 0;
        }
        catch (Exception error) { GD.PushError("BASIN_CARGO_FAILED: " + error); }
        finally
        {
            Release();
            if (GodotObject.IsInstanceValid(_claw)) _claw.ClawFrameChanged -= OnClaw;
            if (GodotObject.IsInstanceValid(level))
            {
                level.RecoveryRequested -= OnRecovery;
                await TestSceneTeardown.FreeSceneAfterAudioDrain(this, level);
            }
            Quit(result);
        }
    }

    private void OnRecovery(string reason) => _recoveries++;
    private void OnClaw(MechanicalClawFrame frame, GrappleSurface2D target)
    {
        if (frame.State == _lastState) return;
        _lastState = frame.State;
        if (frame.State == MechanicalClawState.Latched) _latches++;
        if (frame.State == MechanicalClawState.Released) _releases++;
        if (frame.State == MechanicalClawState.Boost) _boosts++;
        GD.Print($"BASIN_CLAW physics={Engine.GetPhysicsFrames()} state={frame.State} failure={frame.Failure} target={target?.GetPath()} position={_player.GlobalPosition} velocity={_player.Velocity} rope={frame.RopeLength}");
    }
    private static bool Attached(MechanicalClawState state) => state is MechanicalClawState.Latched or
        MechanicalClawState.Reel or MechanicalClawState.Swing or MechanicalClawState.Boost;
    private static void Send(StringName action, bool pressed)
    {
        Input.ParseInputEvent(new InputEventAction { Action = action, Pressed = pressed, Strength = pressed ? 1 : 0 });
        Input.FlushBufferedEvents();
    }
    private static void Release()
    {
        foreach (StringName action in new[] { InputActions.MoveLeft, InputActions.MoveRight, InputActions.MoveUp,
            InputActions.MoveDown, InputActions.Jump, InputActions.Dash, InputActions.PrimaryAction,
            InputActions.Shield, InputActions.SelectGrappleMode, InputActions.SelectAttackMode }) Send(action, false);
    }
    private async Task Frames(int count)
    {
        ulong end = Engine.GetPhysicsFrames() + (ulong)count;
        while (Engine.GetPhysicsFrames() < end)
        {
            Vector2 pointer = _player.GetViewport().GetFinalTransform() *
                (_player.GetGlobalTransformWithCanvas() * _player.ToLocal(_aim));
            var motion = new InputEventMouseMotion { Position = pointer, GlobalPosition = pointer };
            _player.GetViewport().PushInput(motion, true);
            Input.ParseInputEvent(motion);
            Input.FlushBufferedEvents();
            await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        }
    }
    private async Task Until(Func<bool> condition, int budget, string label)
    {
        ulong end = Engine.GetPhysicsFrames() + (ulong)budget;
        while (!condition() && Engine.GetPhysicsFrames() < end) await Frames(1);
        GD.Print($"BASIN_CARGO_STAGE {label} physics={Engine.GetPhysicsFrames()} position={_player.GlobalPosition} velocity={_player.Velocity}");
        TestAssert.True(condition(), label);
    }
    private async Task Settle(StaticBody2D support, int budget)
    {
        int stable = 0; ulong previous = 0;
        await Until(() =>
        {
            ulong tick = Engine.GetPhysicsFrames();
            if (tick == previous) return stable >= 12;
            previous = tick;
            bool contact = false;
            for (int i = 0; i < _player.GetSlideCollisionCount(); i++)
            {
                KinematicCollision2D hit = _player.GetSlideCollision(i);
                contact |= hit.GetCollider() == support && hit.GetNormal().Y < -.7f;
            }
            stable = contact && _player.IsOnFloor() ? stable + 1 : 0;
            return stable >= 12;
        }, budget, "supported in 12 distinct physics-tick samples on " + support.Name);
    }
    private async Task Capture(string stage)
    {
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        if (_output == null) return;
        using Image frame = Root.GetTexture().GetImage();
        TestAssert.Equal(Error.Ok, frame.SavePng(Path.Combine(_output, stage + ".png")), "save capture");
    }
}
