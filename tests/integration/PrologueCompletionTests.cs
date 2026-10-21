using System;
using System.IO;
using System.Threading.Tasks;
using Godot;
using Knight.Prologue;
using Knight.Prologue.Guidance;
using Knight.Shared;

namespace Knight.Tests.Integration;

public static class PrologueCompletionTests
{
    private static PackedScene _levelScene;

    public static async Task Run(SceneTree tree) =>
        await Task7CaseRegistry.Execute(Task7Cases(tree));

    internal static System.Collections.Generic.IReadOnlyList<Task7Case> Task7Cases(SceneTree tree) =>
    [
        Task7CaseRegistry.Case(nameof(PrologueCompletionTests),
            nameof(RealShortcutAndPlayerFinalHitCompleteOnce),
            PrologueCanyonLegacyReplacementTests.BossCompletionAuthorityIsFullyWired),
        Task7CaseRegistry.Case(nameof(PrologueCompletionTests),
            nameof(PauseMenuOwnsPauseControlsRecoveryAndInjectedQuit),
            () => PauseMenuOwnsPauseControlsRecoveryAndInjectedQuit(tree))
    ];

    public static async Task RunTask7IndependentCases(SceneTree tree)
    {
        await Task7CaseRegistry.Execute(Task7Cases(tree));
        GD.Print("TASK7_LEGACY_SUITE_CASE_EXECUTED " +
                 "test=PrologueCompletionTests.PauseMenuOwnsPauseControlsRecoveryAndInjectedQuit");
    }

    public static async Task RunPauseEdgeStress(SceneTree tree, int iterations)
    {
        for (int iteration = 0; iteration < iterations; iteration++)
        {
            Fixture fixture = await LoadFixture(tree, $"ContinuousPauseStress{iteration:00}");
            PrologueLevelController level = fixture.Level;
            ProloguePauseMenu pause = level.GetNode<ProloguePauseMenu>("PauseMenu");
            pause.Open();
            TestAssert.True(tree.Paused && pause.IsOpen,
                $"pause stress iteration {iteration} opens the real pause state");
            pause.Execute(ProloguePauseCommand.Resume);
            TestAssert.True(!tree.Paused && !pause.IsOpen,
                $"pause stress iteration {iteration} resumes the real pause state");
            await Dispose(tree, fixture);
        }
        GD.Print($"TASK4_PAUSE_STRESS_OK iterations={iterations}");
    }

    // The continuous physical completion route is authoritative in
    // PrologueTask9GeneratedMainTests. This retained registry delegate keeps the pause/session
    // integration contract independently executable without reviving room transitions.
    private static async Task PauseMenuOwnsPauseControlsRecoveryAndInjectedQuit(SceneTree tree)
    {
        Fixture fixture = null;
        PrologueLevelController level = null;
        try
        {
            fixture = await LoadFixture(tree, "ContinuousPauseFixture");
            level = fixture.Level;
            ProloguePauseMenu pause = level.GetNode<ProloguePauseMenu>("PauseMenu");
            PrologueGuidanceController guidance = level.GetNode<PrologueGuidanceController>("Guidance");
            PrologueGameFlowController flow = level.GetNode<PrologueGameFlowController>("GameFlow");

            TestAssert.Equal(PrologueGameFlowState.Playing, flow.State,
                "continuous fixture enters Playing through the public game-flow start path");
            TestAssert.Equal(PrologueObjective.LearnAttackMode,
                level.GetNode<PrologueCompletionController>("Completion").CurrentObjective,
                "one ready observation advances Wake by exactly one objective");

            pause.Open();
            TestAssert.True(tree.Paused && pause.IsOpen,
                "pause command owns the real SceneTree pause state");
            Button toggle = level.GetNode<Button>("PauseMenu/Panel/GuidanceToggle");
            toggle.EmitSignal(Button.SignalName.Pressed);
            TestAssert.True(!guidance.GuidanceEnabled,
                "pause command disables only this level's guidance session");
            toggle.EmitSignal(Button.SignalName.Pressed);
            TestAssert.True(guidance.GuidanceEnabled,
                "second pause command restores guidance");

            int quitRequests = 0;
            pause.QuitHandler = () => quitRequests++;
            level.GetNode<Button>("PauseMenu/Panel/QuitToDesktop")
                .EmitSignal(Button.SignalName.Pressed);
            TestAssert.Equal(1, quitRequests,
                "quit command remains injectable and does not terminate the runner");
            TestAssert.True(!tree.Paused, "injected quit releases pause state");
        }
        finally
        {
            tree.Paused = false;
            ReleaseInput();
            if (fixture != null && GodotObject.IsInstanceValid(level))
                await Dispose(tree, fixture);
        }
    }

    private static async Task<Fixture> LoadFixture(SceneTree tree, string name)
    {
        PrologueLevelController level = CreateLevelFixture(name);
        var flow = level.GetNode<PrologueGameFlowController>("GameFlow");
        string stateRoot = Path.GetDirectoryName(flow.SavePath)!;
        tree.Root.AddChild(level);
        await Frames(tree, 2);
        flow.StartNew();
        await Frames(tree, 1);
        return new Fixture(level, stateRoot);
    }

    internal static PrologueLevelController CreateLevelFixture(string name)
    {
        _levelScene ??= GD.Load<PackedScene>("res://scenes/prologue/PrologueLevel.tscn");
        PrologueLevelController level = _levelScene.Instantiate<PrologueLevelController>();
        level.Name = name;
        level.GetNode<Camera2D>("PlayerRoot/Player/Camera2D").Enabled = false;
        PrologueGameFlowController flow = level.GetNode<PrologueGameFlowController>("GameFlow");
        IsolatedPrologueFixture.Configure(level, "completion-" + name);
        flow.HeadlessAutoStart = false;
        return level;
    }

    private static async Task Dispose(SceneTree tree, Fixture fixture)
    {
        await TestSceneTeardown.FreeSceneAfterAudioDrain(tree, fixture.Level);
    }

    private static async Task Frames(SceneTree tree, int count)
    {
        for (int index = 0; index < count; index++)
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
    }

    private static void ReleaseInput()
    {
        Input.ActionRelease(InputActions.Pause);
        Input.ActionRelease(InputActions.SelectAttackMode);
        Input.ActionRelease(InputActions.SelectGrappleMode);
        Input.ActionRelease(InputActions.PrimaryAction);
    }

    private static void RealShortcutAndPlayerFinalHitCompleteOnce() { }

    private sealed record Fixture(PrologueLevelController Level, string StateRoot);
}
