using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Knight.Abilities;
using Knight.Combat;
using Knight.Player;
using Knight.Prologue;
using Knight.Prologue.Guidance;
using Knight.Prologue.Seamless;
using Knight.Settings;
using Knight.Shared;
using Knight.Tests.Integration;
using Knight.Ui;

namespace Knight.Tests.Presentation;

// One generated-main player, starting at the authored Basin entry shelf, or checkpoint with
// --from-ground. No position/velocity writes after mount. Neither is a complete entry-to-Basin run.
public partial class BasinExplorationProbe : SceneTree
{
    private CharacterBody2D _player;
    private string _output;
    private int _recoveries;
    private Vector2? _aim;
    private MechanicalClawController _claw;
    private MechanicalClawState _previousClaw;
    private int _latches, _releases, _boosts;
    private readonly List<string> _guidanceFailures = new();

    public override async void _Initialize()
    {
        PrologueLevelController level = null;
        int exitCode = 1;
        try
        {
            await ToSignal(this, SignalName.ProcessFrame);
            if (DisplayServer.GetName() == "headless") throw new InvalidOperationException("Display required.");
            bool fromGround = Array.Exists(OS.GetCmdlineUserArgs(), a => a == "--from-ground");
            _output = Array.Find(OS.GetCmdlineUserArgs(), a => a.StartsWith("--output="))?[9..];
            if (_output != null)
            {
                if (!Path.IsPathFullyQualified(_output)) throw new ArgumentException("Absolute output required.");
                string project = Path.GetFullPath(ProjectSettings.GlobalizePath("res://")).TrimEnd('/', '\\') + Path.DirectorySeparatorChar;
                _output = Path.GetFullPath(_output);
                if ((_output.TrimEnd('/', '\\') + Path.DirectorySeparatorChar).StartsWith(project, StringComparison.OrdinalIgnoreCase))
                    throw new ArgumentException("Evidence must be outside the project.");
                Directory.CreateDirectory(_output);
            }
            Release();
            level = GD.Load<PackedScene>("res://scenes/prologue/PrologueLevel.tscn").Instantiate<PrologueLevelController>();
            _player = level.GetNode<CharacterBody2D>("PlayerRoot/Player");
            _player.Position = fromGround ? new Vector2(2380, 2014) : new Vector2(2380, 1568);
            _claw = _player.GetNode<MechanicalClawController>("MechanicalClaw");
            _claw.ClawFrameChanged += OnClaw;
            PrologueGameFlowController flow = level.GetNode<PrologueGameFlowController>("GameFlow");
            string isolated = $"user://basin-exploration-probe/{Guid.NewGuid():N}";
            flow.AutomationAutoStart = true;
            flow.SavePath = isolated + "/save.json";
            flow.SettingsPath = isolated + "/settings.json";
            level.RecoveryRequested += OnRecovery;
            Root.AddChild(level);
            await Frames(8);
            if (Array.Exists(OS.GetCmdlineUserArgs(), a => a == "--english"))
            {
                GameSettingsData settings = flow.Settings.Normalize();
                settings.Language = GameLanguage.English;
                flow.ApplySettings(settings);
            }
            TestAssert.Equal(PrologueSectorId.CanyonBasin, level.CurrentSector.Value, "fixture mounts actual Basin");
            level.GetNode<PrologueCompletionController>("Completion").RestoreSession(PrologueObjective.LearnShield);
            HealthComponent health = _player.GetNode<HealthComponent>("Health");
            int heals = 0, healed = 0;
            health.Healed += OnHealed;
            void OnHealed(int amount, int remaining) { heals++; healed += amount; }
            try
            {
                // Public damage prepares missing health; it is not an enemy encounter claim.
                TestAssert.True(health.ApplyDamage(new DamageRequest(fromGround ? 1 : 3, Vector2.Zero, 0, 800001)), "injured supply fixture");
                StaticBody2D entry = level.CurrentSectorRoot.GetNode<StaticBody2D>("Gameplay/Collision/EntryCatch");
                StaticBody2D upper = level.CurrentSectorRoot.GetNode<StaticBody2D>("Gameplay/Collision/SalvageFloor");
                StaticBody2D floor = level.CurrentSectorRoot.GetNode<StaticBody2D>("Gameplay/Collision/BasinFloor");
                if (fromGround) await ClimbFromGround(level, floor, upper);
                else
                {
                    await Settle(entry, 60);
                    await Capture("entry");
                    Send(InputActions.MoveRight, true);
                    await Until(() => _player.GlobalPosition.X >= 2585, 180, "walk off EntryCatch to the optional upper route");
                    Send(InputActions.MoveRight, false);
                    await Settle(upper, 180);
                }
                await Capture("upper-arrival");
                Send(InputActions.MoveRight, true);
                await Until(() => _player.GlobalPosition.X >= 2675, 120, "walk into salvage reward");
                Send(InputActions.MoveRight, false);
                await Frames(16);
                await Capture("reward");
                GD.Print($"BASIN_REWARD_OBSERVED health={health.CurrentHealth} healed={healed} events={heals} discoveries={string.Join(',', level.Progress.Discoveries)}");
                int expectedHealth = fromGround ? 5 : 4;
                int expectedHealing = fromGround ? 1 : 2;
                bool rewarded = health.CurrentHealth == expectedHealth && healed == expectedHealing && heals == 1 &&
                    level.Progress.Discoveries.Contains("salvage_cache");
                Send(InputActions.MoveLeft, true);
                await Until(() => _player.GlobalPosition.X <= 2380, 240, "return off upper left edge to mainline checkpoint");
                Send(InputActions.MoveLeft, false);
                await Settle(floor, 90);
                await Capture("mainline-return");
                TestAssert.Equal(0, _recoveries, "exploration return uses no recovery/teleport");
                TestAssert.Equal(0, _player.GetNode<PlayerMotor>("Motor").AcceptedJumpCount, "exploration needs no ordinary jump");
                TestAssert.Equal(0, _player.GetNode<PlayerMotor>("Motor").AcceptedDashCount, "exploration needs no dash");
                TestAssert.True(rewarded, "actual optional detour heals the missing health once and persists its discovery");
                TestAssert.Equal(expectedHealth, health.CurrentHealth, "return to mainline keeps the earned health");
                if (fromGround)
                {
                    TestAssert.Equal(1, _latches, "one terrain attachment on the return route");
                    TestAssert.Equal(1, _boosts, "one attached Space boost, not an ordinary second jump");
                    TestAssert.Equal(1, _releases, "one intentional release after the boost");
                    CheckGuidance(level, attached: false, "mainline-return");
                    TestAssert.True(_guidanceFailures.Count == 0, string.Join(" | ", _guidanceFailures));
                }
                GD.Print($"BASIN_EXPLORATION_OK entry={(fromGround ? "authored_ground_checkpoint" : "authored_entry_shelf")} real_input=True reward={expectedHealing} return=mainline jump=0 dash=0 recovery=0 boosts={_boosts}");
                exitCode = 0;
            }
            finally { health.Healed -= OnHealed; }
        }
        catch (Exception exception) { GD.PushError("BASIN_EXPLORATION_FAILED: " + exception); }
        finally
        {
            Release();
            if (GodotObject.IsInstanceValid(_claw)) _claw.ClawFrameChanged -= OnClaw;
            if (GodotObject.IsInstanceValid(level))
            {
                level.RecoveryRequested -= OnRecovery;
                await TestSceneTeardown.FreeSceneAfterAudioDrain(this, level);
            }
            Quit(exitCode);
        }
    }

    private async Task ClimbFromGround(PrologueLevelController level, StaticBody2D floor, StaticBody2D upper)
    {
        await Settle(floor, 60);
        _aim = new Vector2(2380, 1632); // inside the existing EntryCatch bottom face
        Send(InputActions.SelectGrappleMode, true);
        await Frames(2);
        Send(InputActions.SelectGrappleMode, false);
        await Capture("ground-aim");
        Send(InputActions.PrimaryAction, true);
        await Frames(1);
        Send(InputActions.PrimaryAction, false);
        await Until(() => _latches == 1, 90, "grapple the existing overhead EntryCatch underside");
        await Frames(2);
        TestAssert.True(_claw.CurrentTarget?.GetParent()?.Name == "EntryCatch", "the actual locked target belongs to the overhead terrain");
        CheckGuidance(level, attached: true, "latched");
        await Capture("ground-latched");
        await Until(() => _player.GlobalPosition.Y <= 1880 || _releases > 0, 240, "auto-pull above the solid right-hand platform lip");
        TestAssert.Equal(0, _releases, "no unintended release before clearing the platform lip");
        CheckGuidance(level, attached: true, "reeled");
        await Capture("ground-reeled");
        Send(InputActions.MoveRight, true);
        Send(InputActions.Jump, true);
        await Until(() => _boosts == 1, 12, "accept one Space boost while attached");
        Send(InputActions.Jump, false);
        await Until(() => _player.GlobalPosition.X >= 2480, 120, "move over the existing salvage platform");
        Send(InputActions.MoveRight, false);
        await Settle(upper, 180);
        await Frames(2);
        CheckGuidance(level, attached: false, "upper-landing");
    }

    private void CheckGuidance(PrologueLevelController level, bool attached, string stage)
    {
        var instruction = level.GetNode<PrologueGuidanceController>("Guidance").CurrentInstruction;
        var visual = level.GetNode<WorldTargetMarkerVisual>("Hud/Guidance/VisualRoot");
        Label label = visual.GetNode<Label>("Text");
        bool complete = instruction.Visible && visual.IsVisibleInTree() && visual.HasVisiblePrompt &&
            label.IsVisibleInTree() && !string.IsNullOrWhiteSpace(instruction.StepText) &&
            label.Text.Contains(instruction.StepText, StringComparison.Ordinal) &&
            visual.PromptRect.Grow(.5f).Encloses(label.GetRect()) && label.GetMinimumSize().Y <= label.Size.Y + .5f;
        bool correct = attached
            ? instruction.Targets.Count == 1 && instruction.Targets[0] == PrologueGuidanceTargetKey.ClawAttachment &&
              visual.RenderedTargetCount == 1 && Array.TrueForAll(new[] { "W", "S", "SPACE", "LMB" },
                  glyph => instruction.StepText.Contains(glyph, StringComparison.OrdinalIgnoreCase))
            : instruction.StepText.Contains("RMB", StringComparison.Ordinal) &&
              !instruction.Targets.Contains(PrologueGuidanceTargetKey.ClawAttachment);
        PrologueObjective objective = level.GetNode<PrologueCompletionController>("Completion").CurrentObjective;
        GD.Print($"BASIN_RETURN_GUIDANCE stage={stage} state={_claw.CurrentFrame.State} objective={objective} complete={complete} correct={correct} targets={string.Join(',', instruction.Targets)} step={instruction.StepText}");
        if (!complete || !correct || objective != PrologueObjective.LearnShield)
            _guidanceFailures.Add(stage + ": guidance must follow actual attachment, then restore the unchanged shield objective");
    }

    private void OnClaw(MechanicalClawFrame frame, GrappleSurface2D target)
    {
        if (frame.State == _previousClaw) return;
        _previousClaw = frame.State;
        if (frame.State == MechanicalClawState.Latched) _latches++;
        if (frame.State == MechanicalClawState.Released) _releases++;
        if (frame.State == MechanicalClawState.Boost) _boosts++;
        GD.Print($"BASIN_RETURN_CLAW physics={Engine.GetPhysicsFrames()} state={frame.State} failure={frame.Failure} target={target?.GetPath()} anchor={frame.AnchorPoint} player={_player.GlobalPosition} velocity={_player.Velocity} rope={frame.RopeLength}");
    }

    private void OnRecovery(string reason) => _recoveries++;
    private static void Send(StringName action, bool pressed)
    {
        Input.ParseInputEvent(new InputEventAction { Action = action, Pressed = pressed, Strength = pressed ? 1 : 0 });
        Input.FlushBufferedEvents();
    }
    private static void Release()
    {
        foreach (StringName action in new[] { InputActions.MoveLeft, InputActions.MoveRight, InputActions.Jump,
            InputActions.Dash, InputActions.PrimaryAction, InputActions.Shield, InputActions.SelectGrappleMode,
            InputActions.SelectAttackMode, InputActions.MoveUp, InputActions.MoveDown }) Send(action, false);
    }
    private async Task Frames(int count)
    {
        ulong end = Engine.GetPhysicsFrames() + (ulong)count;
        while (Engine.GetPhysicsFrames() < end)
        {
            if (_aim is Vector2 aim)
            {
                Vector2 pointer = _player.GetViewport().GetFinalTransform() * (_player.GetGlobalTransformWithCanvas() * _player.ToLocal(aim));
                var motion = new InputEventMouseMotion { Position = pointer, GlobalPosition = pointer };
                _player.GetViewport().PushInput(motion, true);
                Input.ParseInputEvent(motion);
                Input.FlushBufferedEvents();
            }
            await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        }
    }
    private async Task Until(Func<bool> condition, int budget, string stage)
    {
        ulong end = Engine.GetPhysicsFrames() + (ulong)budget;
        while (!condition() && Engine.GetPhysicsFrames() < end) await Frames(1);
        GD.Print($"BASIN_STAGE stage={stage} physics={Engine.GetPhysicsFrames()} position={_player.GlobalPosition} velocity={_player.Velocity}");
        TestAssert.True(condition(), stage + "; player=" + _player.GlobalPosition);
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
            bool contact = false;
            for (int i = 0; i < _player.GetSlideCollisionCount(); i++)
            {
                KinematicCollision2D hit = _player.GetSlideCollision(i);
                contact |= hit.GetCollider() == support && hit.GetNormal().Y < -.7f;
            }
            stable = contact && _player.IsOnFloor() ? stable + 1 : 0;
            return stable >= 12;
        }, budget, "supported in 12 distinct physics samples on " + support.Name);
    }
    private async Task Capture(string stage)
    {
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        if (_output == null) return;
        using Image frame = Root.GetTexture().GetImage();
        TestAssert.Equal(Error.Ok, frame.SavePng(Path.Combine(_output, stage + ".png")), "save evidence frame");
    }
}
