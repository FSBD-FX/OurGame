using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Knight.Prologue;
using Knight.Prologue.Guidance;
using Knight.Shared;

namespace Knight.Tests.Integration;

public static class PrologueCompletionTests
{
    private static PackedScene _levelScene;

    public static async Task RunCurrentPauseQuitOnly(SceneTree tree)
    {
        await AssertFailedLoadOwnership(tree);
        const string name = nameof(PrologueCompletionTests) + "." +
            nameof(PauseMenuOwnsPauseControlsRecoveryAndInjectedQuit);
        Task7Case[] selected = Task7Cases(tree).Where(item => item.Name == name).ToArray();
        TestAssert.Equal(1, selected.Length, "pause-quit probe selects exactly the original registered case");
        Task7CaseRun run = await Task7CaseRegistry.Execute(selected);
        TestAssert.Equal(1, run.Completed.Count, "original pause-quit case completes once");
        GD.Print("THRUSTER_PAUSE_QUIT_OK original_registered_case=true isolated_storage=true human=false full_route=false");
    }

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
            bool pausedInsideQuit = false;
            pause.QuitHandler = () =>
            {
                quitRequests++;
                pausedInsideQuit = tree.Paused && pause.IsOpen &&
                    flow.State == PrologueGameFlowState.Paused;
                pause.Execute(ProloguePauseCommand.QuitToDesktop);
            };
            level.GetNode<Button>("PauseMenu/Panel/QuitToDesktop")
                .EmitSignal(Button.SignalName.Pressed);
            TestAssert.Equal(1, quitRequests,
                "quit command remains injectable and does not terminate the runner");
            pause.Execute(ProloguePauseCommand.QuitToDesktop);
            pause.Execute(ProloguePauseCommand.Resume);
            TestAssert.True(quitRequests == 1 && pausedInsideQuit && tree.Paused && pause.IsOpen &&
                flow.State == PrologueGameFlowState.Paused && !pause.QuitFailureVisible,
                "successful save keeps gameplay paused through one returning/reentrant quit handler");
        }
        finally
        {
            tree.Paused = false;
            ReleaseInput();
            if (fixture != null && GodotObject.IsInstanceValid(level))
                await Dispose(tree, fixture);
        }
    }

    private static async Task AssertFailedLoadOwnership(SceneTree tree)
    {
        foreach (bool failCleanup in new[] { false, true })
        {
            PrologueLevelController owned = null;
            var primary = new InvalidOperationException("injected failure after real Playing");
            var cleanup = new InvalidOperationException("injected cleanup failure before root release");
            Exception observed = null;
            Exception checkFailure = null;
            bool returned = false;
            Node[] otherOwners = tree.Root.GetChildren().ToArray();
            try
            {
                try
                {
                    await LoadFixture(tree, "FailedLoadOwnership" + failCleanup,
                        afterPlaying: level => { owned = level; throw primary; },
                        failureCleanup: failCleanup ? (_, _) => Task.FromException(cleanup) : null);
                    returned = true;
                }
                catch (Exception error) { observed = error; }
                bool expectedFailure = ReferenceEquals(observed, primary) || observed is AggregateException errors &&
                    errors.InnerExceptions.Any(error => ReferenceEquals(error, primary));
                if (!expectedFailure)
                    throw new InvalidOperationException("fault-injection fixture did not reach its real Playing sentinel", observed);
                TestAssert.True(!returned && owned != null && !GodotObject.IsInstanceValid(owned),
                    $"failed load releases its own mounted root even when cleanup fails; cleanup_failure={failCleanup}");
                bool exactFailure = failCleanup
                    ? observed is AggregateException aggregate &&
                        aggregate.InnerExceptions.Count == 2 &&
                        ReferenceEquals(aggregate.InnerExceptions[0], primary) &&
                        ReferenceEquals(aggregate.InnerExceptions[1], cleanup)
                    : ReferenceEquals(observed, primary);
                if (!exactFailure)
                    throw new InvalidOperationException(
                        "failure cleanup preserves exactly the original errors and never accepts fallback errors", observed);
                TestAssert.True(otherOwners.All(owner => GodotObject.IsInstanceValid(owner) && owner.GetParent() == tree.Root),
                    "failure cleanup never removes another mounted owner");
            }
            catch (Exception error) { checkFailure = error; throw; }
            finally
            {
                // RED-path containment belongs to this fault-injection test,
                // never to the caller of a failed LoadFixture.
                if (GodotObject.IsInstanceValid(owned))
                {
                    try { await Dispose(tree, new Fixture(owned, "unused")); }
                    catch (Exception containment)
                    {
                        if (checkFailure != null) throw new AggregateException(checkFailure, containment);
                        throw;
                    }
                }
            }
        }
        GD.Print("THRUSTER_PAUSE_FIXTURE_FAILURE_OWNERSHIP_OK cases=2 real_playing=true primary_preserved=true cleanup_preserved=true other_owners_unchanged=true");
    }

    private static async Task<Fixture> LoadFixture(SceneTree tree, string name,
        Action<PrologueLevelController> afterPlaying = null,
        Func<SceneTree, Fixture, Task> failureCleanup = null)
    {
        PrologueLevelController level = CreateLevelFixture(name);
        var flow = level.GetNode<PrologueGameFlowController>("GameFlow");
        string stateRoot = Path.GetDirectoryName(flow.SavePath)!;
        var fixture = new Fixture(level, stateRoot);
        try
        {
            tree.Root.AddChild(level);
            await Frames(tree, 2);
            flow.StartNew();
            await Frames(tree, 1);
            await BasinAscentCameraRuntimeTests.WaitForPlaying(tree, level);
            afterPlaying?.Invoke(level);
            return fixture;
        }
        catch (Exception primary)
        {
            try
            {
                if (GodotObject.IsInstanceValid(level))
                {
                    if (level.IsInsideTree()) await (failureCleanup ?? Dispose)(tree, fixture);
                    else level.Free();
                }
            }
            catch (Exception cleanup)
            {
                var failures = new System.Collections.Generic.List<Exception> { primary, cleanup };
                void Attempt(Action action)
                {
                    try { action(); }
                    catch (Exception fallback) { failures.Add(fallback); }
                }
                if (GodotObject.IsInstanceValid(level))
                {
                    if (level.IsInsideTree())
                    {
                        // This caller never received its fixture. A failed
                        // drain must not prevent release of this owned root.
                        Attempt(level.CancelResidentPreparation);
                        Attempt(level.QueueFree);
                        try { await Frames(tree, 2); }
                        catch (Exception fallback) { failures.Add(fallback); }
                    }
                    else Attempt(level.Free);
                    if (GodotObject.IsInstanceValid(level))
                        failures.Add(new InvalidOperationException("failed load fallback did not release its owned root"));
                }
                throw new AggregateException(failures);
            }
            throw;
        }
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
