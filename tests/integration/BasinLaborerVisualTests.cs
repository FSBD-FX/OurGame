using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Knight.Enemies.Laborer;
using Knight.Combat;
using Knight.Presentation;
using Knight.Prologue;
using Knight.Prologue.Sample;

namespace Knight.Tests.Integration;

public static class BasinLaborerVisualTests
{
    public static async Task Run(SceneTree tree)
    {
        string directory = Path.Combine(Path.GetTempPath(), "knight-laborer-visual-" + Guid.NewGuid().ToString("N"));
        BasinQualitySampleEntry entry = null;
        try
        {
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            entry = GD.Load<PackedScene>("res://scenes/prologue/sample/BasinQualitySample.tscn")
                .Instantiate<BasinQualitySampleEntry>();
            entry.StorageDirectory = directory;
            tree.Root.AddChild(entry);
            var level = entry.Level;
            await Frames(tree, 5);
            var flow = level.GetNode<PrologueGameFlowController>("GameFlow");
            TestAssert.Equal(PrologueGameFlowState.Title, flow.State,
                "deferred title initialization completes before Start");
            level.GetNode<Button>("ProductUi/MainMenu/Panel/Buttons/Start")
                .EmitSignal(Button.SignalName.Pressed);
            await Frames(tree, 5);
            TestAssert.Equal(PrologueGameFlowState.Playing, flow.State,
                "typed binding test runs in active gameplay, not at the title");
            var enemy = level.CurrentSectorRoot.GetNode<BellBackLaborer>("Gameplay/Actors/BellBackLaborer");
            var visual = enemy.GetNode<Node2D>("VisualRoot/Formal");
            TestAssert.True(visual.GetNodeOrNull<Node2D>("ChestLeft") != null &&
                visual.GetNodeOrNull<Node2D>("ChestRight") != null &&
                visual.GetNodeOrNull<Node2D>("Core") != null,
                "the real sample enemy needs articulated armor that can open to reveal its core");
            TestAssert.True(visual is BasinLaborerVisual articulated &&
                articulated.CurrentState == enemy.CurrentState,
                "the live coordinator must route actual typed states to the sample visual");
            var telegraph = enemy.GetNode<AttackTelegraphVisual>("VisualRoot/AttackTelegraph");
            TestAssert.True(telegraph.ImpactFillOpacity <= .1f &&
                telegraph.ImpactOutlineWidth >= 2f && telegraph.ImpactTint.A >= .95f,
                "sample sweep keeps an opaque boundary without a body-obscuring impact fill");
            var baseline = GD.Load<PackedScene>("res://scenes/enemies/BellBackLaborer.tscn")
                .Instantiate<BellBackLaborer>();
            try
            {
                TestAssert.True(baseline.GetNode("VisualRoot/Formal") is BellBackLaborerFormalVisual,
                    "the default full-game Laborer retains its existing renderer");
                TestAssert.Equal(.34f, baseline.GetNode<AttackTelegraphVisual>(
                    "VisualRoot/AttackTelegraph").ImpactFillOpacity,
                    "default encounter telegraph remains unchanged");
                TestAssert.Equal(baseline.Tuning.ToSettings(), enemy.Tuning.ToSettings() with
                    { DetectionRange = baseline.Tuning.DetectionRange },
                    "sample motion changes no AI/combat timing or damage");
                foreach (string path in new[] { "BodyCollider", "Hurtbox/HurtboxCollider", "AttackHitbox/AttackCollider" })
                {
                    var original = baseline.GetNode<CollisionShape2D>(path).Shape;
                    var current = enemy.GetNode<CollisionShape2D>(path).Shape;
                    if (original is CapsuleShape2D a && current is CapsuleShape2D b)
                        TestAssert.True(a.Radius == b.Radius && a.Height == b.Height, path + " unchanged");
                    else if (original is ConvexPolygonShape2D p && current is ConvexPolygonShape2D q)
                        TestAssert.True(p.Points.SequenceEqual(q.Points), "real sweep polygon unchanged");
                    else throw new InvalidOperationException("Unexpected sample collision replacement: " + path);
                }
            }
            finally { baseline.Free(); }
            await LiveStateContracts(tree, enemy);
            flow.ReturnToTitle();
            await Frames(tree, 3);
            level.GetNode<Button>("ProductUi/MainMenu/Panel/Buttons/Start")
                .EmitSignal(Button.SignalName.Pressed);
            await Frames(tree, 5);
            TestAssert.Equal(PrologueGameFlowState.Playing, flow.State, "sample restarts normally");
            await LiveStateContracts(tree,
                level.CurrentSectorRoot.GetNode<BellBackLaborer>("Gameplay/Actors/BellBackLaborer"));
            await PoseContracts(tree);
        }
        finally
        {
            tree.Paused = false;
            if (GodotObject.IsInstanceValid(entry))
            {
                if (GodotObject.IsInstanceValid(entry.Level))
                    await TestSceneTeardown.FreeSceneAfterAudioDrain(tree, entry.Level);
                entry.QueueFree();
                await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            }
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }

    // Controlled actor integration fixture, not player-input or human-play evidence.
    // It calls real gameplay receivers; never calls ApplyLaborerFrame on the bound rig.
    private static async Task LiveStateContracts(SceneTree tree, BellBackLaborer enemy)
    {
        var visual = enemy.GetNode<BasinLaborerVisual>("VisualRoot/Formal");
        var core = visual.GetNode<Node2D>("Core");
        TestAssert.True(enemy.TryReceiveThread(default), "live actor accepts thread fixture");
        await Frames(tree, 3);
        TestAssert.True(enemy.CurrentState == BellBackLaborerState.Exposed &&
            visual.CurrentState == enemy.CurrentState && core.Visible,
            "actual thread event reaches the coordinator and opens the bound armor");
        await Frames(tree, 90);
        TestAssert.True(enemy.CurrentState != BellBackLaborerState.Exposed &&
            visual.CurrentState == enemy.CurrentState && !core.Visible,
            "actual exposure timeout closes the bound armor");
        TestAssert.True(enemy.GetNode<HealthComponent>("Health").ApplyDamage(
            new DamageRequest(100, Vector2.Zero, 0, 901)), "controlled live death fixture accepted");
        await Frames(tree, 3);
        TestAssert.True(enemy.CurrentState == BellBackLaborerState.Dead &&
            visual.CurrentState == BellBackLaborerState.Dead && !core.Visible,
            "actual health death cancels the bound core and attack cue");
        enemy.ResetForCheckpoint();
        await Frames(tree, 3);
        TestAssert.True(enemy.CurrentState == BellBackLaborerState.Idle &&
            visual.CurrentState == BellBackLaborerState.Idle && !core.Visible,
            "checkpoint reset restores the bound closed pose");
    }

    // Explicit visual-only fixtures: no claim that these are live player actions.
    private static async Task PoseContracts(SceneTree tree)
    {
        var visual = new BasinLaborerVisual();
        tree.Root.AddChild(visual);
        try
        {
            Node2D left = visual.GetNode<Node2D>("ChestLeft"), right = visual.GetNode<Node2D>("ChestRight");
            Node2D core = visual.GetNode<Node2D>("Core"), arm = visual.GetNode<Node2D>("Forearm");
            Node2D hammer = visual.GetNode<Node2D>("Hammer"), feet = visual.GetNode<Node2D>("Feet");
            Present(visual, BellBackLaborerState.Idle);
            await Frames(tree, 10);
            float closed = left.Position.DistanceTo(right.Position);
            Transform2D idleArm = arm.Transform;
            Vector2 idleFeet = feet.Position;
            Present(visual, BellBackLaborerState.Guard);
            await Frames(tree, 10);
            TestAssert.True(arm.Transform != idleArm,
                "Guard must bring the actual forearm into a distinct ready stance");
            Present(visual, BellBackLaborerState.Exposed);
            await Frames(tree, 10);
            TestAssert.True(core.Visible && left.Position.DistanceTo(right.Position) > closed + 12,
                "Exposed physically opens both armor plates and reveals the core");
            Present(visual, BellBackLaborerState.Recover, AttackPhase.Recovery, .4f);
            await Frames(tree, 10);
            TestAssert.True(left.Position.DistanceTo(right.Position) <= closed + 1,
                "Recover is still armored and cannot reuse Exposed geometry");
            Present(visual, BellBackLaborerState.Telegraph, AttackPhase.Telegraph, .9f);
            await Frames(tree, 10);
            Transform2D prepared = hammer.Transform;
            Present(visual, BellBackLaborerState.Attack, AttackPhase.Active, .5f);
            await Frames(tree, 3);
            TestAssert.True(hammer.Position.DistanceTo(prepared.Origin) > 16 ||
                Mathf.Abs(hammer.Rotation - prepared.Rotation) > .5f,
                "real attack progress moves the visible hammer out of its windup pose");
            TestAssert.True(feet.Position.DistanceTo(idleFeet) < 8 && visual.Position == Vector2.Zero,
                "presentation keeps feet anchored and never moves the gameplay root");
            Present(visual, BellBackLaborerState.Attack, AttackPhase.Active, .5f);
            Transform2D rightHammer = hammer.Transform;
            Present(visual, BellBackLaborerState.Attack, AttackPhase.Active, .5f, -1);
            TestAssert.True(visual.Scale.X < 0 && hammer.Transform == rightHammer,
                "facing mirrors the complete local rig once, not the arm a second time");
            var timeline = new AttackTimelineFrame(701, EnemyAttackKind.LaborerSweep,
                AttackPhase.Active, .2f, .1f, new Vector2(30, 60), true);
            Vector2[] worldPoints = [new(100, 50), new(120, 20), new(150, 50), new(120, 80)];
            var published = new AttackPresentationFrame(timeline,
                new AttackShapeFrame(AttackShapeKind.ConvexPolygon, new Vector2(100, 50),
                    new Vector2(50, 30), 0, worldPoints));
            visual.PresentAttack(published);
            TestAssert.True(visual.CurrentAttackFrame.Shape.Points.SequenceEqual(worldPoints),
                "left-facing visual retains the authoritative world-space attack points");
            Present(visual, BellBackLaborerState.Dead);
            await Frames(tree, 10);
            TestAssert.True(visual.CurrentState == BellBackLaborerState.Dead && !core.Visible,
                "death cancels the exposed core cue instead of leaving an attackable glow");
            Present(visual, BellBackLaborerState.Idle);
            await Frames(tree, 10);
            TestAssert.True(left.Position.DistanceTo(right.Position) <= closed + 1 &&
                visual.CurrentState == BellBackLaborerState.Idle,
                "reset to Idle restores closed armor after death");
        }
        finally
        {
            visual.QueueFree();
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        }
    }

    private static void Present(BasinLaborerVisual visual, BellBackLaborerState state,
        AttackPhase phase = AttackPhase.None, float progress = 0, int facing = 1)
    {
        float duration = phase switch { AttackPhase.Telegraph => .75f, AttackPhase.Active => .2f,
            AttackPhase.Recovery => .7f, _ => 0 };
        var timeline = phase == AttackPhase.None ? default : new AttackTimelineFrame(701,
            EnemyAttackKind.LaborerSweep, phase, duration, duration * (1 - progress), Vector2.Zero, false);
        visual.ApplyLaborerFrame(new BellBackLaborerFrame(state, 0,
            state == BellBackLaborerState.Attack,
            state is BellBackLaborerState.Guard or BellBackLaborerState.Telegraph or
                BellBackLaborerState.Attack or BellBackLaborerState.Recover,
            true, timeline), facing);
    }

    private static async Task Frames(SceneTree tree, int count)
    {
        for (int index = 0; index < count; index++)
        {
            await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        }
    }
}
