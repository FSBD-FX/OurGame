using System;
using System.IO;
using System.Threading.Tasks;
using Godot;
using Knight.Abilities;
using Knight.Enemies.Laborer;
using Knight.Player;
using Knight.Prologue;
using Knight.Prologue.Sample;
using Knight.Settings;
using Knight.Shared;

namespace Knight.Tests.Integration;

// Live guide/energy/input integration. Exposure and energy expenditure below are controlled
// fixtures, not a claim of an untampered playthrough; the normal loop is verified separately.
public static class BasinCombatReadinessTests
{
    public static async Task Run(SceneTree tree, string scenario)
    {
        string directory = Path.Combine(Path.GetTempPath(), "knight-readiness-" + Guid.NewGuid().ToString("N"));
        BasinQualitySampleEntry entry = null;
        bool en = Array.Exists(OS.GetCmdlineUserArgs(), arg => arg == "--english");
        try
        {
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            ReleaseAll();
            entry = GD.Load<PackedScene>("res://scenes/prologue/sample/BasinQualitySample.tscn")
                .Instantiate<BasinQualitySampleEntry>();
            entry.StorageDirectory = directory;
            if (en) GameSettingsStore.SaveAtomic(Path.Combine(directory, "settings.json"),
                new GameSettingsData { Language = GameLanguage.English, UiScale = 1.25f });
            tree.Root.AddChild(entry);
            await Frames(tree, 5);
            var level = entry.Level;
            level.GetNode<Button>("ProductUi/MainMenu/Panel/Buttons/Start").EmitSignal(Button.SignalName.Pressed);
            await Frames(tree, 8);
            var player = level.Player;
            var energy = player.GetNode<PlayerEnergyController>("Energy");
            var shield = player.GetNode<PlayerShieldController>("Shield");
            var modes = player.GetNode<PlayerActionModeController>("ActionModes");
            var input = player.GetNode<PlayerInputReader>("Input");
            var combat = player.GetNode<PlayerCombat>("Combat");
            var claw = player.GetNode<MechanicalClawController>("MechanicalClaw");
            var enemy = level.CurrentSectorRoot.GetNode<BellBackLaborer>("Gameplay/Actors/BellBackLaborer");
            var panel = level.GetNode<Control>("SampleGuide/View/Panel");
            var title = panel.GetNode<Label>("Title");
            var keys = panel.GetNode<Label>("Keys");

            if (scenario == "overheat")
            {
                Send(InputActions.Shield, true);
                await Until(tree, () => energy.IsOverheated, 200, "held real shield naturally overheats at camp");
                TestAssert.True(!shield.IsActive && input.Current.ShieldHeld, "overheat closes the actual held shield");
                Send(InputActions.MoveRight, true);
                await Until(tree, () => enemy.CurrentState == BellBackLaborerState.Telegraph, 110,
                    "normal approach reaches actual windup while the shield cools");
                Send(InputActions.MoveRight, false);
                await Frames(tree, 2);
                TestAssert.True(energy.IsOverheated && !shield.IsActive, "windup fixture is still genuinely overheated");
                TestAssert.True(title.Text.Contains(en ? "overheat" : "过热") &&
                    keys.Text.Contains("A / D") && !keys.Text.Contains(en ? "HOLD RMB" : "按住右键"),
                    "overheated windup must offer movement, not an unavailable shield");
                AssertFits(panel, scenario);
                Send(InputActions.Shield, false);
                await Frames(tree, 2);
                TestAssert.True(energy.IsOverheated && title.Text.Contains(en ? "overheat" : "过热") &&
                    keys.Text.Contains("A / D") && !keys.Text.Contains(en ? "RELEASE RMB" : "松开右键"),
                    "evade warning remains but already released shield must not retain a stale release instruction");
                await Until(tree, () => enemy.CurrentState == BellBackLaborerState.Attack, 80, "actual sweep follows windup");
                await Frames(tree, 2);
                TestAssert.True(energy.IsOverheated && !shield.IsActive &&
                    title.Text.Contains(en ? "overheat" : "过热") && keys.Text.Contains("A / D"),
                    "an actual active sweep still offers evasion while shield is overheated");
                AssertFits(panel, scenario + "-active-sweep");
                await Until(tree, () => !energy.IsOverheated, 200, "real heat cooldown completes");
                TestAssert.True(enemy.TryReceiveThread(default), "open armor after cooldown fixture");
                await Frames(tree, 3);
                TestAssert.True(!title.Text.Contains(en ? "overheat" : "过热"), "old overheat teaching clears with real recovery");
            }
            else if (scenario == "depleted")
            {
                Send(InputActions.MoveRight, true);
                await Until(tree, () => enemy.CurrentState == BellBackLaborerState.Telegraph, 110,
                    "normal approach reaches windup for controlled zero-energy expenditure");
                Send(InputActions.MoveRight, false);
                foreach (var state in new[] { BellBackLaborerState.Telegraph, BellBackLaborerState.Attack })
                {
                    await Until(tree, () => enemy.CurrentState == state, 80, "actual enemy reaches " + state);
                    TestAssert.True(energy.TrySpendHeavyGuard(energy.EnergyRatio * energy.Tuning.MaxEnergy),
                        "controlled accepted guard expenditure reaches exact zero");
                    await Frames(tree, 2);
                    TestAssert.True(energy.EnergyRatio == 0f && !energy.IsOverheated && !shield.IsActive,
                        "zero-energy fixture is distinct from overheat");
                    TestAssert.True(title.Text.Contains(en ? "No shield energy" : "气力耗尽") &&
                        keys.Text.Contains("A / D") && !keys.Text.Contains(en ? "RELEASE RMB" : "松开右键"),
                        "depleted threat offers evasion without requesting an already released button: " + state);
                    AssertFits(panel, scenario + "-" + state);
                }
                await Until(tree, () => energy.EnergyRatio > 0f, 90, "actual energy regeneration resumes");
                await Frames(tree, 2);
                TestAssert.True(!title.Text.Contains(en ? "No shield energy" : "气力耗尽"),
                    "zero-energy warning clears after actual recovery");
            }
            else
            {
                if (scenario == "claw_hot")
                {
                    Send(InputActions.Shield, true);
                    await Until(tree, () => energy.IsOverheated, 200, "held shield naturally overheats before claw fixture");
                }
                TestAssert.True(enemy.TryReceiveThread(default), "real enemy receiver opens armor for guidance fixture");
                await Frames(tree, 3);
                if (scenario is "energy" or "energy_held")
                {
                    for (int i = 0; i < 8; i++) TestAssert.True(energy.TrySpendAttack(), "controlled accepted attack expenditure");
                    await Frames(tree, 2);
                    if (scenario == "energy_held")
                    {
                        Send(InputActions.Shield, true);
                        await Until(tree, () => energy.EnergyRatio == 0f, 30,
                            "actual held shield drains the remaining low energy to zero");
                        await Frames(tree, 2);
                        TestAssert.True(input.Current.ShieldHeld && !shield.IsActive && !energy.IsOverheated,
                            "exposed empty-energy shield is inactive but RMB is still held");
                        TestAssert.True(keys.Text.Contains(en ? "RELEASE RMB" : "松开右键"),
                            "recovery instruction releases held shield before it repeatedly consumes regenerated energy");
                        AssertFits(panel, scenario + "-held");
                        Send(InputActions.Shield, false);
                        await Frames(tree, 2);
                    }
                    TestAssert.True(energy.EnergyRatio * energy.Tuning.MaxEnergy < 12f && !modes.CanBeginAttack,
                        "energy fixture cannot afford the actual attack");
                    TestAssert.True(title.Text.Contains(en ? "energy" : "气力") &&
                        !title.Text.Contains(en ? "strike now" : "现在反击"),
                        "exposure must not promise an unaffordable attack");
                    TestAssert.True(!input.Current.PrimaryHeld && !input.Current.ShieldHeld &&
                        !keys.Text.Contains(en ? "RELEASE" : "松开"),
                        "already released inputs show recovery advice, not a stale release command");
                    AssertFits(panel, scenario);
                    await Until(tree, () => modes.CanBeginAttack, 90, "actual regeneration makes attack affordable");
                    await Frames(tree, 2);
                    TestAssert.True(!title.Text.Contains(en ? "energy" : "气力"), "regenerated energy clears unavailable-action teaching");
                }
                else if (scenario == "shield")
                {
                    Send(InputActions.Shield, true);
                    await Frames(tree, 5);
                    TestAssert.True(shield.IsActive && !modes.CanBeginAttack, "live held shield blocks attack");
                    TestAssert.True(keys.Text.Contains(en ? "RELEASE RMB" : "松开右键"),
                        "exposure instructs release before an attack that a held shield blocks");
                    AssertFits(panel, scenario);
                    Send(InputActions.Shield, false);
                    await Frames(tree, 2);
                    Send(InputActions.PrimaryAction, true);
                    await Frames(tree, 2);
                    TestAssert.True(combat.IsAttacking, "following the indicated release then fresh click actually starts attack");
                }
                else if (scenario == "slot")
                {
                    Send(InputActions.Slot2, true);
                    await Frames(tree, 2);
                    Send(InputActions.Slot2, false);
                    TestAssert.True(modes.ActiveSlot == 2 && !modes.CanBeginAttack, "selected empty slot really blocks attack");
                    TestAssert.True(keys.Text.Contains("1") && title.Text.Contains(en ? "empty" : "空"),
                        "empty loadout teaching must identify the usable slot, not only Q mode");
                    AssertFits(panel, scenario);
                    Send(InputActions.Slot1, true);
                    await Frames(tree, 2);
                    Send(InputActions.Slot1, false);
                    TestAssert.True(modes.CanBeginAttack && !title.Text.Contains(en ? "empty" : "空"),
                        "selecting the actual weapon clears the unavailable-slot teaching");
                }
                else if (scenario is "claw" or "claw_hot")
                {
                    Send(InputActions.SelectGrappleMode, true);
                    Send(InputActions.Shield, true);
                    await Frames(tree, 3);
                    Send(InputActions.SelectGrappleMode, false);
                    await Until(tree, () => enemy.CurrentState != BellBackLaborerState.Exposed, 100, "armor closes naturally");
                    await Frames(tree, 2);
                    TestAssert.True(input.Current.ShieldHeld && !modes.CanBeginGrapple && claw.CurrentFrame.State == MechanicalClawState.Idle,
                        "held shield input blocks claw deployment even before a failed click");
                    if (scenario == "claw_hot") TestAssert.True(energy.IsOverheated && !shield.IsActive,
                        "held RMB still blocks claw while the actual shield is overheated and inactive");
                    TestAssert.True(keys.Text.Contains(en ? "RELEASE RMB" : "松开右键"),
                        "claw teaching identifies the held shield barrier");
                    AssertFits(panel, scenario);
                    Send(InputActions.Shield, false);
                    await Frames(tree, 2);
                    TestAssert.True(modes.CanBeginGrapple && !keys.Text.Contains(en ? "RELEASE RMB" : "松开右键"),
                        "release restores actual claw availability and normal teaching");
                }
                else throw new ArgumentException("Unknown readiness scenario: " + scenario);
            }
            AssertFits(panel, scenario + "-resolved");
            GD.Print($"BASIN_COMBAT_READINESS_CASE_OK case={scenario} english={en}");
        }
        finally
        {
            ReleaseAll();
            tree.Paused = false;
            if (GodotObject.IsInstanceValid(entry))
            {
                if (GodotObject.IsInstanceValid(entry.Level)) await TestSceneTeardown.FreeSceneAfterAudioDrain(tree, entry.Level);
                entry.QueueFree();
                await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            }
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }
    }

    private static void AssertFits(Control panel, string scenario)
    {
        foreach (string name in new[] { "Title", "Keys" })
        {
            var label = panel.GetNode<Label>(name);
            float width = label.GetThemeFont("font").GetStringSize(label.Text,
                HorizontalAlignment.Left, -1, label.GetThemeFontSize("font_size")).X;
            TestAssert.True(label.IsVisibleInTree() && width <= label.Size.X + 1f &&
                label.Position.X + label.Size.X <= panel.Size.X,
                $"visible readiness instruction fits: {scenario}/{name} width={width}/{label.Size.X}");
        }
        var body = panel.GetNode<Label>("Body");
        TestAssert.True(!body.Visible || body.Position.Y + body.Size.Y <= panel.Size.Y &&
            body.GetMinimumSize().Y <= body.Size.Y,
            "visible readiness explanation fits its card: " + scenario);
    }

    private static void Send(StringName action, bool pressed)
    {
        Input.ParseInputEvent(new InputEventAction { Action = action, Pressed = pressed, Strength = pressed ? 1 : 0 });
        Input.FlushBufferedEvents();
    }
    private static void ReleaseAll()
    {
        foreach (StringName action in new[] { InputActions.MoveLeft, InputActions.MoveRight, InputActions.Shield,
            InputActions.PrimaryAction, InputActions.SelectAttackMode, InputActions.SelectGrappleMode, InputActions.Slot1, InputActions.Slot2 })
            Send(action, false);
    }
    private static async Task Frames(SceneTree tree, int count)
    {
        for (int i = 0; i < count; i++)
        {
            await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        }
    }
    private static async Task Until(SceneTree tree, Func<bool> ready, int limit, string reason)
    {
        for (int i = 0; i < limit && !ready(); i++) await Frames(tree, 1);
        TestAssert.True(ready(), reason);
    }
}
