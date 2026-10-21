using System;
using System.IO;
using System.Threading.Tasks;
using Godot;
using Knight.Abilities;
using Knight.Combat;
using Knight.Enemies.Laborer;
using Knight.Player;
using Knight.Prologue;
using Knight.Prologue.Sample;
using Knight.Shared;
using Knight.Settings;
using Knight.Presentation;

namespace Knight.Tests.Presentation;

// Automated normal-window input proof, not human playtest evidence. It changes only the
// sample StorageDirectory / isolated presentation settings before mount and never writes
// gameplay state, position or velocity.
public partial class BasinQualitySampleLoopProbe : SceneTree
{
    private string _output;
    private CharacterBody2D _player;
    private BellBackLaborer _laborer;
    private HealthComponent _playerHealth;
    private HealthComponent _enemyHealth;
    private PlayerShieldController _shield;
    private PlayerEnergyController _energy;
    private PlayerCombat _combat;
    private PlayerInputReader _input;
    private PlayerActionModeController _modes;
    private PlayerMotor _motor;
    private MechanicalClawController _claw;
    private LegacyThreadInteractionAdapter _laborerThread;
    private PrologueSupplyCache _supply;
    private AttackPresentationFrame _attackFrame;
    private Node2D _dynamicAim;
    private Vector2? _fixedAim;
    private BellBackLaborerState _previousLaborerState;
    private MechanicalClawState _previousClawState;
    private int _playerDamageEvents;
    private int _playerMinimumHealth = int.MaxValue;
    private int _blocks;
    private int _exposures;
    private int _enemyDamageEvents;
    private int _enemyDefeats;
    private int _attacksStarted;
    private int _attacksConnected;
    private int _enemyClawApplications;
    private int _terrainLatches;
    private int _clawReleases;
    private int _boosts;
    private int _clawFailures;
    private int _heals;
    private int _healedAmount;
    private int _recoveries;
    private string _lastClawTarget = "None";

    public override async void _Initialize()
    {
        BasinQualitySampleEntry entry = null;
        PrologueLevelController level = null;
        int exitCode = 1;
        try
        {
            await ToSignal(this, SignalName.ProcessFrame);
            if (DisplayServer.GetName() == "headless")
                throw new InvalidOperationException("Display required for the sample loop probe.");
            _output = ResolveOutput();
            Directory.CreateDirectory(_output);
            ReleaseAll();

            entry = GD.Load<PackedScene>("res://scenes/prologue/sample/BasinQualitySample.tscn")
                .Instantiate<BasinQualitySampleEntry>();
            entry.StorageDirectory = Path.Combine(
                _output, "isolated-slot-" + Guid.NewGuid().ToString("N"));
            bool english = Array.Exists(OS.GetCmdlineUserArgs(), argument => argument == "--english");
            bool largeUi = Array.Exists(OS.GetCmdlineUserArgs(), argument => argument == "--large-ui");
            if (english || largeUi)
                GameSettingsStore.SaveAtomic(Path.Combine(entry.StorageDirectory, "settings.json"),
                    new GameSettingsData { Language = english ? GameLanguage.English :
                        GameLanguage.ChineseSimplified, UiScale = largeUi ? 1.25f : 1f });
            Root.AddChild(entry);
            level = entry.GetNode<PrologueLevelController>("Level");
            _player = level.GetNode<CharacterBody2D>("PlayerRoot/Player");
            await Frames(30);
            TestAssert.Equal(PrologueGameFlowState.Title,
                level.GetNode<PrologueGameFlowController>("GameFlow").State,
                "real sample opens at title before loop playback");
            await Capture("01-title", level);
            level.GetNode<Button>("ProductUi/MainMenu/Panel/Buttons/Start")
                .EmitSignal(Button.SignalName.Pressed);
            await Frames(8);

            Bind(level);
            TestAssert.True(level.CurrentSector == Knight.Prologue.Seamless.PrologueSectorId.CanyonBasin,
                "real sample Start mounts the authored Basin");
            StaticBody2D basinFloor = level.CurrentSectorRoot.GetNode<StaticBody2D>(
                "Gameplay/Collision/BasinFloor");
            StaticBody2D entryCatch = level.CurrentSectorRoot.GetNode<StaticBody2D>(
                "Gameplay/Collision/EntryCatch");
            StaticBody2D salvageFloor = level.CurrentSectorRoot.GetNode<StaticBody2D>(
                "Gameplay/Collision/SalvageFloor");
            _supply = level.CurrentSectorRoot.GetNode<PrologueSupplyCache>(
                "Gameplay/OptionalPaths/SalvageCache/RecoverySupply");
            await Frames(60);
            await SettleOn(basinFloor, 180, "camp ground");
            await Capture("02-camp", level);
            AssertGuide(level, 0, "camp shows combat guidance");

            if (Array.Exists(OS.GetCmdlineUserArgs(), argument => argument == "--overheat-approach"))
                await ApproachWhileOverheated(level, basinFloor);
            await DefendAndDefeatLaborer(level);
            await Frames(2);
            AssertGuide(level, 1, "real defeat unlocks climb guidance");
            await Capture("06-laborer-defeated", level);
            await ClimbCollectAndReturn(level, basinFloor, entryCatch, salvageFloor);

            TestAssert.Equal(1, _enemyDefeats, "one real Laborer defeat is required");
            TestAssert.Equal(0, _recoveries, "the loop cannot use death/fall recovery");
            TestAssert.True(_blocks >= 1 && _exposures >= 1 && _enemyClawApplications >= 1,
                "combat must include a real zero-damage block and thread exposure");
            TestAssert.True(_enemyHealth.CurrentHealth == 0 && _enemyDamageEvents >= 6 &&
                _attacksStarted >= 6 && _attacksConnected >= 6,
                "real exposed attacks must remove all six Laborer health");
            TestAssert.True(_terrainLatches >= 1 && _boosts >= 1 && _clawReleases >= 1,
                "return loop must attach to EntryCatch, boost, and release normally");
            TestAssert.True(_player.IsOnFloor() &&
                MathF.Abs(_player.GlobalPosition.X - 2380f) <= 120f &&
                MathF.Abs(_player.GlobalPosition.Y - 2014f) <= 8f,
                "loop ends grounded back at the authored Basin camp");
            TestAssert.True(_clawFailures == 0,
                "successful combat and terrain casts cannot hide claw failures");
            Log(level, "complete");
            AssertGuide(level, 4, "normal grounded return completes only the sample guide");
            await Capture("10-returned-camp", level);
            GD.Print($"BASIN_SAMPLE_LOOP_PROOF_OK human=False real_input=True defeated={_enemyDefeats} " +
                $"damage={_playerDamageEvents} block={_blocks} expose={_exposures} " +
                $"enemyhealth={_enemyHealth.CurrentHealth} enemy_damage={_enemyDamageEvents} " +
                $"enemy_claw={_enemyClawApplications} terrain_latch={_terrainLatches} " +
                $"boost={_boosts} discovery={_supply.IsSpent} healed={_healedAmount} " +
                $"player={_player.GlobalPosition} recovery={_recoveries}");
            exitCode = 0;
        }
        catch (Exception exception)
        {
            GD.PushError("BASIN_SAMPLE_LOOP_PROOF_FAILED: " + exception);
        }
        finally
        {
            ReleaseAll();
            Paused = false;
            Unbind(level);
            if (GodotObject.IsInstanceValid(level))
                await TestSceneTeardown.FreeSceneAfterAudioDrain(this, level);
            if (GodotObject.IsInstanceValid(entry))
            {
                entry.QueueFree();
                await ToSignal(this, SignalName.ProcessFrame);
            }
            Quit(exitCode);
        }
    }

    private async Task ApproachWhileOverheated(PrologueLevelController level, StaticBody2D floor)
    {
        _dynamicAim = _laborer;
        SendMouse(MouseButton.Right, true);
        await Until(() => _energy.IsOverheated, 200, level, "ordinary held shield overheats at camp");
        SendAction(InputActions.MoveRight, true);
        await Until(() => _laborer.CurrentState == BellBackLaborerState.Telegraph,
            110, level, "approach while shield is still overheated");
        SendAction(InputActions.MoveRight, false);
        TestAssert.True(_energy.IsOverheated && !_shield.IsActive,
            "normal stress approach reaches windup with an actually unavailable shield");
        await Capture("02-overheated-threat", level);
        bool en = level.GetNode<PrologueGameFlowController>("GameFlow").Settings.Language == GameLanguage.English;
        TestAssert.True(level.GetNode<Label>("SampleGuide/View/Panel/Title").Text.Contains(en ? "overheat" : "过热"),
            "normal gameplay displays the unavailable-shield warning");
        SendMouse(MouseButton.Right, false);
        await Frames(2);
        var threatKeys = level.GetNode<Label>("SampleGuide/View/Panel/Keys");
        TestAssert.True(_energy.IsOverheated && threatKeys.Text.Contains("A / D") &&
            !threatKeys.Text.Contains("RELEASE RMB") && !threatKeys.Text.Contains("松开右键"),
            "ordinary button release removes the stale release prefix while evasion remains necessary");
        await WalkToX(2380f, 6f, 180, level, "follow evade instruction back toward camp");
        await SettleOn(floor, 120, "grounded after ordinary evade");
        await Until(() => !_energy.IsOverheated, 200, level, "ordinary cooldown restores shield availability");
    }

    private async Task DefendAndDefeatLaborer(PrologueLevelController level)
    {
        _dynamicAim = _laborer;
        await SelectAttackMode();
        await KeepCombatDistance(70f, 360);
        await Until(() => _attackFrame.Timeline.Phase == AttackPhase.Telegraph,
            300, level, "Laborer Telegraph");
        await Capture("03-telegraph", level);
        await Frames(Math.Max(1, Engine.PhysicsTicksPerSecond / 4));
        SendMouse(MouseButton.Right, true);
        await Until(() => _shield.IsActive, 45, level, "shield raised from real RMB");
        await Frames(6);
        await Capture("03-guard", level);
        await Until(() => _blocks >= 1, 150, level, "zero-damage sweep block");
        SendMouse(MouseButton.Right, false);
        await Until(() => !_shield.IsActive, 45, level, "shield released after sweep");
        await Capture("04-blocked-sweep", level);

        for (int round = 1; round <= 4 && _enemyHealth.CurrentHealth > 0; round++)
        {
            await KeepCombatDistance(58f, 180);
            int applicationsBefore = _laborerThread.ApplicationCount;
            await SelectGrappleMode();
            SendMouse(MouseButton.Left, true);
            await Frames(1);
            SendMouse(MouseButton.Left, false);
            await Until(() => _laborerThread.ApplicationCount > applicationsBefore,
                120, level, "thread opens Laborer armor round " + round);
            await Until(() => _laborer.CurrentState == BellBackLaborerState.Exposed,
                30, level, "Laborer enters Exposed round " + round);
            TestAssert.Equal(new NodePath("../Health"),
                _laborer.GetNode<Hurtbox>("Hurtbox").DamageReceiverPath,
                "Exposed Laborer routes damage directly to Health");
            await Capture("05-exposed-" + round, level);
            await SelectAttackMode();

            while (_enemyHealth.CurrentHealth > 0 &&
                   _laborer.CurrentState == BellBackLaborerState.Exposed)
            {
                if (_modes.CanBeginAttack && !_combat.IsAttacking)
                    await AttackTowardLaborer(level);
                else
                    await Frames(1);
            }
            Log(level, "exposure-round-" + round);
        }

        await Until(() => _enemyDefeats == 1 &&
                _laborer.CurrentState == BellBackLaborerState.Dead,
            90, level, "Laborer real Defeated event");
    }

    private async Task ClimbCollectAndReturn(PrologueLevelController level,
        StaticBody2D basinFloor, StaticBody2D entryCatch, StaticBody2D salvageFloor)
    {
        _dynamicAim = null;
        await WalkToX(2380f, 6f, 240, level, "return to camp under EntryCatch");
        await SettleOn(basinFloor, 120, "pre-climb BasinFloor");
        _fixedAim = new Vector2(2380f, 1632f);
        await SelectGrappleMode();
        int terrainBefore = _terrainLatches;
        SendMouse(MouseButton.Left, true);
        await Frames(1);
        SendMouse(MouseButton.Left, false);
        await Until(() => _terrainLatches > terrainBefore,
            120, level, "grapple EntryCatch underside");
        TestAssert.True(_claw.CurrentTarget?.GetParent() == entryCatch,
            "live claw target belongs to authored EntryCatch terrain");
        await Capture("07-entrycatch-attached", level);
        await Until(() => _player.GlobalPosition.Y <= 1880f,
            240, level, "auto-pull above platform lip");
        SendAction(InputActions.MoveRight, true);
        SendAction(InputActions.Jump, true);
        int boostsBefore = _boosts;
        await Until(() => _boosts > boostsBefore, 20, level, "attached Space boost");
        SendAction(InputActions.Jump, false);
        await Until(() => _player.GlobalPosition.X >= 2480f,
            150, level, "cross onto salvage platform");
        SendAction(InputActions.MoveRight, false);
        await SettleOn(salvageFloor, 210, "SalvageFloor landing");
        AssertGuide(level, 2, "real shelf landing unlocks supply guidance");
        await Capture("08-salvage-platform", level);

        int healthBeforeSupply = _playerHealth.CurrentHealth;
        SendAction(InputActions.MoveRight, true);
        await Until(() => _player.GlobalPosition.X >= 2675f,
            150, level, "walk through SalvageCache");
        SendAction(InputActions.MoveRight, false);
        await Frames(20);
        if (healthBeforeSupply < _playerHealth.MaxHealth)
        {
            TestAssert.True(_supply.IsSpent &&
                level.Progress.Discoveries.Contains("salvage_cache") &&
                _heals == 1 && _healedAmount > 0 &&
                _playerHealth.CurrentHealth > healthBeforeSupply,
                "injured run consumes the real supply and heals normally");
        }
        else
        {
            TestAssert.True(!_supply.IsSpent &&
                !level.Progress.Discoveries.Contains("salvage_cache") &&
                _heals == 0 && _playerHealth.CurrentHealth == _playerHealth.MaxHealth,
                "full-health run preserves the supply without fabricated damage");
        }
        await Capture("09-salvage-cache", level);
        AssertGuide(level, 3, "real supply visit shows the safe return route");

        _fixedAim = null;
        SendAction(InputActions.MoveLeft, true);
        await Until(() => _player.GlobalPosition.X <= 2380f,
            240, level, "walk off upper route toward camp");
        SendAction(InputActions.MoveLeft, false);
        await SettleOn(basinFloor, 180, "returned camp BasinFloor");
        await WalkToX(2380f, 60f, 120, level, "center returned camp");
        await SettleOn(basinFloor, 90, "centered camp BasinFloor");
    }

    private static void AssertGuide(PrologueLevelController level, int expected, string reason)
    {
        var guide = level.GetNode<BasinSampleGuideController>("SampleGuide");
        TestAssert.Equal(expected, guide.Step, reason);
        TestAssert.True(guide.GetNode<CanvasLayer>("View").Visible &&
            guide.GetNode<Control>("View/Panel").Visible &&
            !string.IsNullOrWhiteSpace(guide.GetNode<Label>("View/Panel/Body").Text),
            reason + ": the actual card must be visible and populated");
        foreach (string labelName in new[] { "Title", "Keys" })
        {
            var label = guide.GetNode<Label>("View/Panel/" + labelName);
            float textWidth = label.GetThemeFont("font").GetStringSize(label.Text,
                HorizontalAlignment.Left, -1, label.GetThemeFontSize("font_size")).X;
            TestAssert.True(textWidth <= label.Size.X + 1,
                reason + $": {labelName} must not truncate instructions ({textWidth}/{label.Size.X})");
        }
        var panel = guide.GetNode<Control>("View/Panel");
        var body = guide.GetNode<Label>("View/Panel/Body");
        var readout = guide.GetNode<Control>("View/CombatReadout");
        var actor = level.CurrentSectorRoot.GetNode<BellBackLaborer>("Gameplay/Actors/BellBackLaborer");
        if (body.Visible)
            TestAssert.True(body.Position.Y + body.Size.Y <= panel.Size.Y,
                reason + ": visible wrapped body must remain inside the panel");
        else
            TestAssert.True(expected == 0 && readout.Visible && panel.Size.Y <= 100f &&
                actor.CurrentState is BellBackLaborerState.Telegraph or BellBackLaborerState.Attack or BellBackLaborerState.Exposed,
                reason + ": compact teaching requires urgent, visible local combat information");
        if (!readout.Visible) return;
        var state = readout.GetNode<Label>("State");
        var health = actor.GetNode<HealthComponent>("Health");
        var bar = readout.GetNode<ProgressBar>("Health");
        TestAssert.True(bar.Value == health.CurrentHealth && bar.MaxValue == health.MaxHealth &&
            bar.Size.Y <= 6f && bar.Position.Y + bar.Size.Y <= readout.Size.Y,
            reason + ": local health must be real, thin and contained");
        bool english = level.GetNode<PrologueGameFlowController>("GameFlow").Settings.Language == GameLanguage.English;
        string expectedState = actor.CurrentState switch
        {
            BellBackLaborerState.Telegraph => english ? "WINDING UP" : "横扫蓄力",
            BellBackLaborerState.Attack => english ? "SWEEP ACTIVE" : "横扫中",
            BellBackLaborerState.Exposed => english ? "ARMOR OPEN" : "开甲",
            BellBackLaborerState.Recover => english ? "RECOVERING · ARMORED" : "收招 · 仍有甲",
            _ => english ? "ARMORED" : "有甲"
        };
        TestAssert.Equal(expectedState, state.Text, reason + ": local label must match the real enemy phase");
        float stateWidth = state.GetThemeFont("font").GetStringSize(state.Text,
            HorizontalAlignment.Left, -1, state.GetThemeFontSize("font_size")).X;
        TestAssert.True(stateWidth <= state.Size.X && state.Position.X + state.Size.X <= readout.Size.X &&
            !readout.GetGlobalRect().Intersects(panel.GetGlobalRect()),
            reason + ": local phase must fit without colliding with the teaching card");
    }

    private async Task AttackTowardLaborer(PrologueLevelController level)
    {
        StringName direction = _laborer.GlobalPosition.X >= _player.GlobalPosition.X
            ? InputActions.MoveRight : InputActions.MoveLeft;
        SendAction(direction, true);
        SendMouse(MouseButton.Left, true);
        await Frames(1);
        if (_attacksStarted == 1)
        {
            await Frames(4);
            await Capture("05-attack", level);
        }
        SendMouse(MouseButton.Left, false);
        SendAction(direction, false);
        await UntilWithoutLog(() => !_combat.IsAttacking || _enemyHealth.CurrentHealth == 0, 45);
    }

    private async Task KeepCombatDistance(float target, int budget)
    {
        int stable = 0;
        ulong end = Engine.GetPhysicsFrames() + (ulong)budget;
        while (stable < 5 && Engine.GetPhysicsFrames() < end)
        {
            float separation = _laborer.GlobalPosition.X - _player.GlobalPosition.X;
            if (MathF.Abs(separation) <= target + 10f)
            {
                SendAction(InputActions.MoveLeft, false);
                SendAction(InputActions.MoveRight, false);
                stable++;
            }
            else
            {
                stable = 0;
                bool moveRight = separation > target;
                SendAction(InputActions.MoveRight, moveRight);
                SendAction(InputActions.MoveLeft, !moveRight);
            }
            await Frames(1);
        }
        SendAction(InputActions.MoveLeft, false);
        SendAction(InputActions.MoveRight, false);
        TestAssert.True(stable >= 5,
            "normal movement could not establish Laborer combat distance; player=" +
            _player.GlobalPosition + " enemy=" + _laborer.GlobalPosition);
    }

    private async Task WalkToX(float target, float tolerance, int budget,
        PrologueLevelController level, string stage)
    {
        ulong end = Engine.GetPhysicsFrames() + (ulong)budget;
        while (MathF.Abs(_player.GlobalPosition.X - target) > tolerance &&
               Engine.GetPhysicsFrames() < end)
        {
            bool right = _player.GlobalPosition.X < target;
            SendAction(InputActions.MoveRight, right);
            SendAction(InputActions.MoveLeft, !right);
            await Frames(1);
        }
        SendAction(InputActions.MoveLeft, false);
        SendAction(InputActions.MoveRight, false);
        Log(level, stage);
        TestAssert.True(MathF.Abs(_player.GlobalPosition.X - target) <= tolerance,
            stage + " timed out; player=" + _player.GlobalPosition);
    }

    private async Task SelectAttackMode()
    {
        SendAction(InputActions.SelectAttackMode, true);
        SendAction(InputActions.Slot1, true);
        await Frames(1);
        SendAction(InputActions.SelectAttackMode, false);
        SendAction(InputActions.Slot1, false);
        await UntilWithoutLog(() => _modes == null ||
            _modes.CurrentMode == PlayerActionMode.Attack && _modes.ActiveSlot == 1, 20);
    }

    private async Task SelectGrappleMode()
    {
        SendAction(InputActions.SelectGrappleMode, true);
        await Frames(1);
        SendAction(InputActions.SelectGrappleMode, false);
        await UntilWithoutLog(() => _modes.CurrentMode == PlayerActionMode.Grapple, 20);
    }

    private async Task SettleOn(StaticBody2D support, int budget, string stage)
    {
        int stable = 0;
        ulong previousPhysics = ulong.MaxValue;
        await UntilWithoutLog(() =>
        {
            ulong physics = Engine.GetPhysicsFrames();
            if (physics == previousPhysics) return stable >= 12;
            previousPhysics = physics;
            bool contact = false;
            for (int index = 0; index < _player.GetSlideCollisionCount(); index++)
            {
                KinematicCollision2D collision = _player.GetSlideCollision(index);
                contact |= collision.GetCollider() == support && collision.GetNormal().Y < -.7f;
            }
            stable = _player.IsOnFloor() && contact ? stable + 1 : 0;
            return stable >= 12;
        }, budget);
        TestAssert.True(stable >= 12,
            stage + " did not retain twelve distinct grounded support samples; player=" +
            _player.GlobalPosition);
    }

    private async Task Until(Func<bool> condition, int budget,
        PrologueLevelController level, string stage)
    {
        await UntilWithoutLog(condition, budget);
        Log(level, stage);
        TestAssert.True(condition(), stage + " timed out; player=" + _player.GlobalPosition);
    }

    private async Task UntilWithoutLog(Func<bool> condition, int budget)
    {
        ulong end = Engine.GetPhysicsFrames() + (ulong)budget;
        while (!condition() && Engine.GetPhysicsFrames() < end) await Frames(1);
    }

    private async Task Frames(int count)
    {
        ulong end = Engine.GetPhysicsFrames() + (ulong)count;
        while (Engine.GetPhysicsFrames() < end)
        {
            PushAimMotion();
            await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        }
    }

    private void Bind(PrologueLevelController level)
    {
        _laborer = level.CurrentSectorRoot.GetNode<BellBackLaborer>(
            "Gameplay/Actors/BellBackLaborer");
        _playerHealth = _player.GetNode<HealthComponent>("Health");
        _enemyHealth = _laborer.GetNode<HealthComponent>("Health");
        _shield = _player.GetNode<PlayerShieldController>("Shield");
        _energy = _player.GetNode<PlayerEnergyController>("Energy");
        _combat = _player.GetNode<PlayerCombat>("Combat");
        _input = _player.GetNode<PlayerInputReader>("Input");
        _modes = _player.GetNode<PlayerActionModeController>("ActionModes");
        _motor = _player.GetNode<PlayerMotor>("Motor");
        _claw = _player.GetNode<MechanicalClawController>("MechanicalClaw");
        _laborerThread = _laborer.GetNode<LegacyThreadInteractionAdapter>(
            "ThreadTarget/LegacyThreadInteractionAdapter");
        _playerMinimumHealth = _playerHealth.CurrentHealth;
        _previousLaborerState = _laborer.CurrentState;
        _previousClawState = _claw.CurrentFrame.State;
        _playerHealth.Damaged += OnPlayerDamaged;
        _playerHealth.Healed += OnPlayerHealed;
        _shield.ShieldResolved += OnShieldResolved;
        _laborer.FrameChanged += OnLaborerFrame;
        _laborer.AttackFrameChanged += OnAttackFrame;
        _laborer.Defeated += OnLaborerDefeated;
        _enemyHealth.Damaged += OnEnemyDamaged;
        _combat.AttackStarted += OnAttackStarted;
        _combat.AttackConnected += OnAttackConnected;
        _claw.ClawFrameChanged += OnClawFrame;
        level.RecoveryRequested += OnRecovery;
    }

    private void Unbind(PrologueLevelController level)
    {
        if (GodotObject.IsInstanceValid(_playerHealth))
        {
            _playerHealth.Damaged -= OnPlayerDamaged;
            _playerHealth.Healed -= OnPlayerHealed;
        }
        if (GodotObject.IsInstanceValid(_shield)) _shield.ShieldResolved -= OnShieldResolved;
        if (GodotObject.IsInstanceValid(_laborer))
        {
            _laborer.FrameChanged -= OnLaborerFrame;
            _laborer.AttackFrameChanged -= OnAttackFrame;
            _laborer.Defeated -= OnLaborerDefeated;
        }
        if (GodotObject.IsInstanceValid(_enemyHealth)) _enemyHealth.Damaged -= OnEnemyDamaged;
        if (GodotObject.IsInstanceValid(_combat))
        {
            _combat.AttackStarted -= OnAttackStarted;
            _combat.AttackConnected -= OnAttackConnected;
        }
        if (GodotObject.IsInstanceValid(_claw)) _claw.ClawFrameChanged -= OnClawFrame;
        if (GodotObject.IsInstanceValid(level)) level.RecoveryRequested -= OnRecovery;
    }

    private void OnPlayerDamaged(int remaining, Vector2 impulse)
    {
        _playerDamageEvents++;
        _playerMinimumHealth = Math.Min(_playerMinimumHealth, remaining);
    }

    private void OnPlayerHealed(int restored, int remaining)
    {
        _heals++;
        _healedAmount += restored;
    }

    private void OnShieldResolved(PlayerShieldResult result, ulong transition)
    {
        if (result.Guarded && result.DamageAmount == 0) _blocks++;
    }

    private void OnLaborerFrame(BellBackLaborerFrame frame, int facing)
    {
        if (frame.State == BellBackLaborerState.Exposed &&
            _previousLaborerState != BellBackLaborerState.Exposed) _exposures++;
        _previousLaborerState = frame.State;
    }

    private void OnAttackFrame(AttackPresentationFrame frame) => _attackFrame = frame;
    private void OnLaborerDefeated() => _enemyDefeats++;
    private void OnEnemyDamaged(int remaining, Vector2 impulse) => _enemyDamageEvents++;
    private void OnAttackStarted(ulong attackId, int direction) => _attacksStarted++;

    private void OnAttackConnected(Hurtbox hurtbox)
    {
        if (GodotObject.IsInstanceValid(_laborer) &&
            hurtbox == _laborer.GetNodeOrNull<Hurtbox>("Hurtbox")) _attacksConnected++;
    }

    private void OnClawFrame(MechanicalClawFrame frame, GrappleSurface2D target)
    {
        MechanicalClawState previous = _previousClawState;
        if (target != null && GodotObject.IsInstanceValid(target))
            _lastClawTarget = target.GetPath().ToString();
        if (frame.State == MechanicalClawState.Latched &&
            _previousClawState != MechanicalClawState.Latched)
        {
            if (target?.GetParent() == _laborer) _enemyClawApplications++;
            else if (target?.GetParent()?.Name == "EntryCatch") _terrainLatches++;
        }
        if (frame.State == MechanicalClawState.Released &&
            _previousClawState != MechanicalClawState.Released) _clawReleases++;
        if (frame.State == MechanicalClawState.Boost &&
            _previousClawState != MechanicalClawState.Boost) _boosts++;
        if (frame.State == MechanicalClawState.Failed) _clawFailures++;
        if (frame.State != previous || frame.Failure != MechanicalClawFailure.None)
        {
            PlayerInputFrame input = _input?.CurrentFrame ?? default;
            GD.Print($"BASIN_SAMPLE_LOOP_CLAW physics={Engine.GetPhysicsFrames()} " +
                $"previous={previous} state={frame.State} failure={frame.Failure} " +
                $"castid={frame.CastId} mode={_modes?.CurrentMode} " +
                $"primary_pressed={input.PrimaryPressed} primary_held={input.PrimaryHeld} " +
                $"target={(GodotObject.IsInstanceValid(target) ? target.GetPath().ToString() : "None")} " +
                $"laborer={_laborer?.CurrentState} enemyhealth={_enemyHealth?.CurrentHealth}");
        }
        _previousClawState = frame.State;
    }

    private void OnRecovery(string reason) => _recoveries++;

    private void PushAimMotion()
    {
        if (!GodotObject.IsInstanceValid(_player)) return;
        Vector2? aim = GodotObject.IsInstanceValid(_dynamicAim)
            ? _dynamicAim.GlobalPosition : _fixedAim;
        if (!aim.HasValue) return;
        Vector2 pointer = _player.GetViewport().GetFinalTransform() *
            (_player.GetGlobalTransformWithCanvas() * _player.ToLocal(aim.Value));
        var motion = new InputEventMouseMotion { Position = pointer, GlobalPosition = pointer };
        _player.GetViewport().PushInput(motion, true);
        Input.ParseInputEvent(motion);
        Input.FlushBufferedEvents();
    }

    private void SendMouse(MouseButton button, bool pressed)
    {
        PushAimMotion();
        Vector2 pointer = GodotObject.IsInstanceValid(_player)
            ? _player.GetNode<PlayerInputReader>("Input").LastObservedMouseViewportPosition
            : Vector2.Zero;
        var mouse = new InputEventMouseButton
        {
            ButtonIndex = button,
            Pressed = pressed,
            Position = pointer,
            GlobalPosition = pointer,
            ButtonMask = pressed
                ? button == MouseButton.Left ? MouseButtonMask.Left : MouseButtonMask.Right
                : (MouseButtonMask)0
        };
        Input.ParseInputEvent(mouse);
        Input.FlushBufferedEvents();
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

    private void ReleaseAll()
    {
        foreach (StringName action in new[]
                 {
                     InputActions.MoveLeft, InputActions.MoveRight, InputActions.MoveUp,
                     InputActions.MoveDown, InputActions.Jump, InputActions.Dash,
                     InputActions.SelectAttackMode, InputActions.SelectGrappleMode,
                     InputActions.Slot1, InputActions.Slot2, InputActions.Slot3, InputActions.Slot4,
                     InputActions.Pause
                 })
            SendAction(action, false);
        SendMouse(MouseButton.Left, false);
        SendMouse(MouseButton.Right, false);
    }

    private async Task Capture(string stage, PrologueLevelController level)
    {
        await Frames(1);
        if (stage == "03-guard")
        {
            var playerRig = _player.GetNode<PlayerFormalVisual>("VisualRoot/Formal");
            TestAssert.True(_shield.IsActive &&
                playerRig.CurrentPlayerFrame.Action.State == PlayerActionState.Shield &&
                Mathf.Abs(playerRig.CurrentPose.NearArm.RotationRadians) > .35f,
                "captured normal RMB guard reaches the player's actual arm pose");
        }
        if (GodotObject.IsInstanceValid(_laborer))
        {
            var rig = _laborer.GetNode<BasinLaborerVisual>("VisualRoot/Formal");
            TestAssert.Equal(_laborer.CurrentState, rig.CurrentState,
                "captured normal-play state reaches articulated rig: " + stage);
            TestAssert.Equal(_laborer.CurrentState == BellBackLaborerState.Exposed,
                rig.GetNode<Node2D>("Core").Visible, "live armor state: " + stage);
        }
        using Image frame = Root.GetTexture().GetImage();
        TestAssert.True(frame != null && !frame.IsEmpty() &&
            frame.SavePng(Path.Combine(_output, stage + ".png")) == Error.Ok,
            "capture " + stage);
        Log(level, "capture-" + stage);
        var guide = level.GetNode<BasinSampleGuideController>("SampleGuide");
        if (guide.GetNode<CanvasLayer>("View").Visible)
            AssertGuide(level, guide.Step, "captured " + stage);
    }

    private void Log(PrologueLevelController level, string stage)
    {
        GD.Print($"BASIN_SAMPLE_LOOP_STAGE stage={stage} physics={Engine.GetPhysicsFrames()} " +
            $"player={_player?.GlobalPosition} velocity={_player?.Velocity} " +
            $"health={_playerHealth?.CurrentHealth} min_health={_playerMinimumHealth} " +
            $"damage={_playerDamageEvents} block={_blocks} laborer={_laborer?.CurrentState} " +
            $"enemyhealth={_enemyHealth?.CurrentHealth} enemy_damage={_enemyDamageEvents} " +
            $"expose={_exposures} applications={_laborerThread?.ApplicationCount} " +
            $"attacks={_attacksStarted} connections={_attacksConnected} " +
            $"claw={_claw?.CurrentFrame.State} clawtarget={_lastClawTarget} " +
            $"terrain_latch={_terrainLatches} release={_clawReleases} boost={_boosts} " +
            $"discovery={level?.Progress.Discoveries.Contains("salvage_cache")} " +
            $"used={_supply?.IsSpent} heals={_heals} healed={_healedAmount} " +
            $"jump={_motor?.AcceptedJumpCount} recovery={_recoveries}");
    }

    private string ResolveOutput()
    {
        string output = Array.Find(OS.GetCmdlineUserArgs(), argument =>
            argument.StartsWith("--output=", StringComparison.Ordinal))?[9..];
        if (string.IsNullOrWhiteSpace(output) || !Path.IsPathFullyQualified(output))
            throw new ArgumentException("An absolute --output outside the project is required.");
        output = Path.GetFullPath(output);
        string project = Path.GetFullPath(ProjectSettings.GlobalizePath("res://"))
            .TrimEnd('/', '\\') + Path.DirectorySeparatorChar;
        if ((output.TrimEnd('/', '\\') + Path.DirectorySeparatorChar)
            .StartsWith(project, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Evidence must be outside the project.");
        return output;
    }
}
