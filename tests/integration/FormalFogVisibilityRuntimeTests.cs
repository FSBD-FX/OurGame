using System;
using System.Threading.Tasks;
using Godot;
using Knight.Player;
using Knight.Presentation.Fog;
using Knight.Prologue;
using Knight.Settings;

namespace Knight.Tests.Integration;

/// <summary>Actual saved-level pixels, without forcing the formal canvas visible.</summary>
public static class FormalFogVisibilityRuntimeTests
{
    public static async Task Run(SceneTree tree)
    {
        var level = GD.Load<PackedScene>("res://scenes/prologue/PrologueLevel.tscn")
            .Instantiate<PrologueLevelController>();
        try
        {
            IsolatedPrologueFixture.Configure(level, "formal-fog-visibility");
            var flow = level.GetNode<PrologueGameFlowController>("GameFlow");
            flow.HeadlessAutoStart = false; flow.AutomationAutoStart = false;
            tree.Root.AddChild(level);
            await Frames(tree, 5);
            var canvas = level.GetNode<PrologueFogCanvas>("Fog/Canvas");
            var layer = level.GetNode<CanvasLayer>("Fog");
            var controller = level.GetNode<PrologueFogController>("FogController");
            TestAssert.True(flow.State == PrologueGameFlowState.Title && !canvas.Visible,
                "saved Title never exposes uninitialized gameplay fog");
            flow.StartNew();
            await Frames(tree, 8);
            level.Player.GetNode<PlayerMotor>("Motor").SetPhysicsProcess(false);
            level.Player.Velocity = Vector2.Zero;
            TestAssert.True(flow.State == PrologueGameFlowState.Playing && canvas.FrameValid &&
                canvas.Visible && canvas.IsVisibleInTree() && controller.PresentedFrameCount > 0,
                "formal Playing must actually display its own valid fog after protected cues, not only submit hidden uniforms");

            // The sole negative control hides the containing layer. It never
            // forces Canvas.Visible, replaces the controller, world or shader,
            // nor adds a white test backdrop over the actual saved environment.
            layer.Hide();
            using Image clear = await Pixels(tree, level);
            layer.Show();
            using Image normal = await Pixels(tree, level);
            GameSettingsData reduced = flow.Settings.Normalize(); reduced.ReducedFog = true;
            flow.ApplySettings(reduced);
            using Image lighter = await Pixels(tree, level);
            TestAssert.True(clear.GetSize() == normal.GetSize() && normal.GetSize() == lighter.GetSize(),
                "negative control and both actual accessibility settings use the same viewport");
            int darkened = 0, accessible = 0, nearUnchanged = 0;
            Transform2D pixelToWorld = (level.GetViewport().GetFinalTransform() *
                level.GetNode<Node2D>("WorldRoot").GetCanvasTransform()).AffineInverse();
            for (int y = 170; y < normal.GetHeight() - 140; y += 4)
                for (int x = 24; x < normal.GetWidth() - 24; x += 4)
                {
                    Vector2 world = pixelToWorld * new Vector2(x + .5f, y + .5f);
                    float distance = world.DistanceTo(level.Player.GlobalPosition);
                    float a = Brightness(clear.GetPixel(x, y)), b = Brightness(normal.GetPixel(x, y));
                    float c = Brightness(lighter.GetPixel(x, y));
                    // Broad evidence of the real render composition, not a new
                    // duplicate specification of the separately tested shader.
                    if (distance > 300f && a > .08f && a - b > .012f)
                    {
                        darkened++;
                        if (c > b + .004f && Math.Abs(c - (a + b) * .5f) < .018f) accessible++;
                    }
                    if (distance < 130f && controller.LastOcclusionFrame.CanSee(level.Player.GlobalPosition, world) &&
                        a > .08f && Math.Abs(a - b) < .012f) nearUnchanged++;
                }
            TestAssert.True(darkened >= 40 && accessible >= darkened * .75f && nearUnchanged >= 10,
                $"saved world pixels show distance fog, half-opacity accessibility and a clear actor neighborhood; far={darkened} reduced={accessible} near={nearUnchanged}");
            foreach (string name in new[] { "Fragments", "Attacks", "Interaction", "Terrain", "Steam", "ShieldLane", "PressureGates", "MovingBodies", "Barriers" })
            {
                var consumer = level.GetNode<Node2D>("ProtectedCues/" + name);
                consumer.Hide(); await Frames(tree, 2);
                TestAssert.True(!canvas.Visible, $"missing visible {name} protection keeps the world bright");
                consumer.Show(); await Frames(tree, 2);
                TestAssert.True(canvas.Visible && canvas.FrameValid, $"restored {name} permits a new protected formal frame");
            }
            var pause = level.GetNode<ProloguePauseMenu>("PauseMenu");
            pause.Open(); await Frames(tree, 3);
            TestAssert.True(tree.Paused && !canvas.Visible, "Pause hides gameplay fog instead of retaining a stale submission");
            pause.Execute(ProloguePauseCommand.Resume); await Frames(tree, 3);
            TestAssert.True(canvas.Visible && canvas.FrameValid, "Resume displays a fresh valid formal frame");
            flow.ReturnToTitle(); await Frames(tree, 3);
            TestAssert.True(flow.State == PrologueGameFlowState.Title && !canvas.Visible,
                "return to Title revokes formal fog visibility");
            GD.Print($"FORMAL_FOG_SAVED_WORLD_PIXELS_OK far={darkened} reduced={accessible} near={nearUnchanged} fixture=true human=false");
        }
        finally
        {
            tree.Paused = false;
            if (GodotObject.IsInstanceValid(level))
            {
                if (level.IsInsideTree()) await TestSceneTeardown.FreeSceneAfterAudioDrain(tree, level);
                else level.Free();
            }
        }
    }

    private static float Brightness(Color value) => Math.Max(value.R, Math.Max(value.G, value.B));
    private static async Task<Image> Pixels(SceneTree tree, Node level)
    {
        await Frames(tree, 3);
        return level.GetViewport().GetTexture().GetImage();
    }
    private static async Task Frames(SceneTree tree, int count)
    {
        for (int index = 0; index < count; index++)
        {
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            await tree.ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        }
    }
}
