using System.Threading.Tasks;
using Godot;
using Knight.Player;
using Knight.Presentation;
using Knight.Presentation.Fog;
using Knight.Prologue;

namespace Knight.Tests.Integration;

/// <summary>Actual saved parent/mounted-sector wiring, not visible formal fog acceptance.</summary>
public static class FogTerrainSavedControllerRuntimeTests
{
    public static async Task Run(SceneTree tree)
    {
        var level = GD.Load<PackedScene>("res://scenes/prologue/PrologueLevel.tscn").Instantiate<PrologueLevelController>();
        try
        {
            IsolatedPrologueFixture.Configure(level, "fog-saved-terrain");
            var flow = level.GetNode<PrologueGameFlowController>("GameFlow");
            flow.HeadlessAutoStart = false; flow.AutomationAutoStart = false;
            tree.Root.AddChild(level); await Frames(tree, 5);
            var terrain = level.GetNodeOrNull<FogTerrainReadabilityOverlay>("ProtectedCues/Terrain");
            TestAssert.True(terrain != null, "saved formal parent must contain its own layer2 terrain protection");
            var layer = level.GetNode<CanvasLayer>("ProtectedCues");
            TestAssert.True(layer.Layer == 2 && layer.FollowViewportEnabled && layer.FollowViewportScale == 1f &&
                terrain.PresentedSegmentCount == 0 && terrain.LastOcclusionFrame == null,
                "saved terrain protection starts empty at Title on the world-following protected layer");
            flow.StartNew(); await Frames(tree, 8);
            var controller = level.GetNode<PrologueFogController>("FogController");
            var presentation = level.Player.GetNode<PlayerPresentationCoordinator>("Presentation");
            TestAssert.True(flow.State == PrologueGameFlowState.Playing && terrain.PresentedSegmentCount > 0 &&
                ReferenceEquals(controller.LastOcclusionFrame, terrain.LastOcclusionFrame) &&
                presentation.FrameObserverFailureCount == 0 && level.GetNode<PrologueFogCanvas>("Fog/Canvas").Visible,
                "real saved controller submits mounted original support skins with its exact current native frame and visible formal fog");
            var skin = level.CurrentSectorRoot.GetNode<CanyonPlatformVisual>("VisualRoot/GameplayReadable/StartFloor");
            int withStart = terrain.PresentedSegmentCount;
            skin.Hide(); await Frames(tree, 2);
            TestAssert.True(terrain.PresentedSegmentCount < withStart,
                "saved collector does not retain the hidden original StartFloor skin");
            skin.Show(); await Frames(tree, 2);
            TestAssert.Equal(withStart, terrain.PresentedSegmentCount, "original saved source restores without a cached retired list");
            var pause = level.GetNode<ProloguePauseMenu>("PauseMenu");
            pause.Open(); await Frames(tree, 2);
            TestAssert.True(terrain.PresentedSegmentCount == 0 && terrain.LastOcclusionFrame == null,
                "real Pause withdraws all saved terrain commands and frame provenance");
            pause.Execute(ProloguePauseCommand.Resume); await Frames(tree, 2);
            TestAssert.True(terrain.PresentedSegmentCount > 0 && ReferenceEquals(controller.LastOcclusionFrame, terrain.LastOcclusionFrame),
                "real Resume submits fresh current support geometry");
            flow.ReturnToTitle(); await Frames(tree, 2);
            TestAssert.True(terrain.PresentedSegmentCount == 0 && terrain.LastOcclusionFrame == null &&
                !level.GetNode<PrologueFogCanvas>("Fog/Canvas").Visible,
                "return to Title clears terrain and hides the formal fog");
            GD.Print("HEADLAMP_FOG_CONTROLLER_SAVED_TERRAIN_OK pixels=false fixture=true traversal=false formal_playing_visible=true final_title_visible=false");
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
    private static async Task Frames(SceneTree tree, int count)
    {
        for (int i = 0; i < count; i++)
        {
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            await tree.ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        }
    }
}
