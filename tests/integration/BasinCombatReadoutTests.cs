using System;
using System.IO;
using System.Threading.Tasks;
using Godot;
using Knight.Combat;
using Knight.Enemies.Laborer;
using Knight.Prologue;
using Knight.Prologue.Sample;
using Knight.Shared;
using Knight.Ui;

namespace Knight.Tests.Integration;

public static class BasinCombatReadoutTests
{
    public static async Task Run(SceneTree tree)
    {
        string directory = Path.Combine(Path.GetTempPath(), "knight-combat-readout-" + Guid.NewGuid().ToString("N"));
        BasinQualitySampleEntry entry = null;
        try
        {
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            entry = GD.Load<PackedScene>("res://scenes/prologue/sample/BasinQualitySample.tscn")
                .Instantiate<BasinQualitySampleEntry>();
            entry.StorageDirectory = directory;
            tree.Root.AddChild(entry);
            await Frames(tree, 6);
            var level = entry.Level;
            var view = level.GetNode<CanvasLayer>("SampleGuide/View");
            var readout = view.GetNodeOrNull<BasinCombatReadout>("CombatReadout");
            TestAssert.True(readout != null,
                "sample must provide a local enemy phase/health readout instead of only the distant teaching card");
            TestAssert.True(!readout.Visible, "title never exposes a stale combat readout");
            var flow = level.GetNode<PrologueGameFlowController>("GameFlow");
            level.GetNode<Button>("ProductUi/MainMenu/Panel/Buttons/Start").EmitSignal(Button.SignalName.Pressed);
            await Frames(tree, 8);
            TestAssert.Equal(PrologueGameFlowState.Playing, flow.State, "readout fixture starts actual gameplay");
            var enemy = level.CurrentSectorRoot.GetNode<BellBackLaborer>("Gameplay/Actors/BellBackLaborer");
            var label = readout.GetNode<Label>("State");
            var healthBar = readout.GetNode<ProgressBar>("Health");
            var body = view.GetNode<Label>("Panel/Body");
            TestAssert.True(!readout.Visible && body.Visible, "idle camp keeps full teaching and no enemy HUD");

            PushMove(true);
            await Until(tree, () => enemy.CurrentState is BellBackLaborerState.Guard or
                BellBackLaborerState.Telegraph, 150, "normal approach engages the real enemy");
            PushMove(false);
            await Until(tree, () => enemy.CurrentState == BellBackLaborerState.Telegraph, 180, "real windup");
            await Frames(tree, 2);
            TestAssert.True(readout.Visible && label.Text.Contains("蓄力") && !body.Visible,
                "windup appears beside the enemy and shortens the teaching card");
            TestAssert.True(healthBar.Size.Y <= 6f && healthBar.Position.Y + healthBar.Size.Y <= readout.Size.Y,
                $"live health strip must stay thin and inside its panel; size={healthBar.Size}, minimum={healthBar.GetCombinedMinimumSize()}");
            var shelfShape = level.CurrentSectorRoot.GetNode<CollisionShape2D>(
                "Gameplay/Collision/SalvageFloor/Collision");
            float shelfBottom = (shelfShape.GetGlobalTransformWithCanvas() *
                new Vector2(0, ((RectangleShape2D)shelfShape.Shape).Size.Y * .5f)).Y;
            TestAssert.True(readout.GetGlobalRect().Position.Y >= shelfBottom - 1f,
                "local combat panel must not cover the upper route's real platform edge");
            float rearDistance = (readout.GetGlobalRect().GetCenter().X -
                enemy.GetGlobalTransformWithCanvas().Origin.X) * -enemy.Facing;
            TestAssert.True(rearDistance >= readout.Size.X * .5f + 10f,
                "readout must sit behind the enemy rather than covering its frontal sweep apex");
            // Synchronous display-boundary fixture; restore before the next physics frame.
            // The ordinary-window loop separately verifies untampered player movement.
            Vector2 savedPosition = enemy.GlobalPosition;
            try
            {
                enemy.GlobalPosition = enemy.GetCanvasTransform().AffineInverse() * new Vector2(640f, 64f);
                ((BasinSampleGuideView)view).PresentCombat(enemy, false);
                TestAssert.True(!readout.Visible && body.Visible,
                    "a clipped phase label must restore full teaching, even if the health strip is onscreen");
                enemy.GlobalPosition = enemy.GetCanvasTransform().AffineInverse() * new Vector2(1260f, 560f);
                ((BasinSampleGuideView)view).PresentCombat(enemy, false);
                TestAssert.True(!readout.Visible && body.Visible,
                    "a side-clipped readout must not shorten teaching or shift onto another subject");
            }
            finally
            {
                enemy.GlobalPosition = savedPosition;
                ((BasinSampleGuideView)view).PresentCombat(enemy, false);
            }
            await Until(tree, () => enemy.CurrentState == BellBackLaborerState.Attack, 180, "real active sweep");
            await Frames(tree, 2);
            TestAssert.True(readout.Visible && label.Text.Contains("横扫中") &&
                view.GetNode<Label>("Panel/Title").Text.Contains("横扫中"),
                "active damage must never still be described as an incoming sweep");
            await Until(tree, () => enemy.CurrentState == BellBackLaborerState.Recover, 90, "real recovery");
            await Frames(tree, 2);
            TestAssert.True(readout.Visible && label.Text.Contains("有甲") && body.Visible,
                "recovery is still armored, not a fabricated exposed damage window");
            readout.Present(enemy, true, view.GetNode<Control>("Panel").GetGlobalRect());
            float englishWidth = label.GetThemeFont("font").GetStringSize(label.Text,
                HorizontalAlignment.Left, -1, label.GetThemeFontSize("font_size")).X;
            TestAssert.True(readout.Visible && englishWidth <= label.Size.X &&
                label.Position.X + label.Size.X <= readout.Size.X,
                $"English recovery label must fit its panel; text={englishWidth}, label={label.Size.X}");

            // Controlled live receiver/health fixtures; the normal route separately proves player attacks.
            TestAssert.True(enemy.TryReceiveThread(default), "real receiver accepts exposure fixture");
            await Frames(tree, 3);
            TestAssert.True(readout.Visible && label.Text.Contains("开甲") && !body.Visible,
                "only actual exposure advertises the damage opportunity");
            // Controlled opposite-side fixture verifies the live actor's real facing, not a UI test override.
            Vector2 savedPlayerPosition = level.Player.GlobalPosition;
            try
            {
                level.Player.GlobalPosition = enemy.GlobalPosition + new Vector2(70f, 0f);
                // Exposed/guarded idle poses retain facing; the next real windup commits the new direction.
                await Until(tree, () => enemy.CurrentState == BellBackLaborerState.Telegraph &&
                    enemy.Facing == 1, 180, "opposite-side windup commits the real facing");
                await Frames(tree, 2);
                TestAssert.True(enemy.Facing == 1 && readout.Visible &&
                    readout.GetGlobalRect().GetCenter().X < enemy.GetGlobalTransformWithCanvas().Origin.X - 100f,
                    "right-facing enemy keeps local information behind its real attack direction too");
            }
            finally { level.Player.GlobalPosition = savedPlayerPosition; }
            TestAssert.True(enemy.TryReceiveThread(default), "reopen real armor after opposite-side display fixture");
            await Frames(tree, 3);
            var health = enemy.GetNode<HealthComponent>("Health");
            TestAssert.True(health.ApplyDamage(new DamageRequest(1, Vector2.Zero, 0, 7701)), "controlled damage accepted");
            await Frames(tree, 2);
            TestAssert.True(healthBar.Value == health.CurrentHealth && healthBar.MaxValue == health.MaxHealth &&
                healthBar.Value == 5, "local health follows accepted enemy damage rather than attack attempts");
            var pause = level.GetNode<ProloguePauseMenu>("PauseMenu");
            pause.Open();
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            TestAssert.True(!readout.Visible, "pause hides local combat information");
            pause.Execute(ProloguePauseCommand.Resume);
            await Frames(tree, 3);
            TestAssert.True(readout.Visible && healthBar.Value == 5, "resume rebinds live state without restoring health");
            await Frames(tree, 90);
            TestAssert.True(enemy.CurrentState != BellBackLaborerState.Exposed && !label.Text.Contains("开甲"),
                "actual exposure timeout removes the opportunity immediately");
            TestAssert.True(health.ApplyDamage(new DamageRequest(100, Vector2.Zero, 0, 7702)), "controlled death accepted");
            await Frames(tree, 3);
            TestAssert.True(!readout.Visible && body.Visible, "defeat removes local HUD and restores exploration teaching");
            flow.ReturnToTitle();
            await Frames(tree, 3);
            level.GetNode<Button>("ProductUi/MainMenu/Panel/Buttons/Start").EmitSignal(Button.SignalName.Pressed);
            await Frames(tree, 8);
            TestAssert.True(!readout.Visible && body.Visible, "new game clears the previous encounter presentation");
        }
        finally
        {
            PushMove(false);
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

    private static void PushMove(bool pressed)
    {
        Input.ParseInputEvent(new InputEventAction { Action = InputActions.MoveRight, Pressed = pressed, Strength = pressed ? 1 : 0 });
        Input.FlushBufferedEvents();
    }
    private static async Task Frames(SceneTree tree, int count)
    { for (int i = 0; i < count; i++) await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame); }
    private static async Task Until(SceneTree tree, Func<bool> condition, int limit, string message)
    {
        for (int i = 0; i < limit && !condition(); i++) await Frames(tree, 1);
        TestAssert.True(condition(), message);
    }
}
