using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Knight.Boss;
using Knight.Combat;
using Knight.Prologue;
using Knight.Prologue.Seamless;
using Knight.Save;
using Knight.Shared;

namespace Knight.Tests.Integration;

/// <summary>Controlled normal-world exit fixture, never a normal lesson/combat/route proof.</summary>
public static class NormalWorldPhysicalExitTests
{
    // Independent authored geometry: Temple offset(5120,720), ArenaFloor center(1760,416)
    // size(1600,40), Exit center x2180 width36, actual BodyCollider radius20 height72.
    private const float FloorLeft = 6080, FloorRight = 7680, FloorTop = 1116;
    private const float DoorRight = 7318, Radius = 20;
    private const string Target = "normal boss defeat must wait for supported physical exit";

    // Break caught: the success latch/signal/UI commits before the atomic save.
    // This is a controlled Continue/pose fixture with engine action input, not a
    // human keyboard route or a Boss combat proof. The actual Windows file lock
    // allows preflight reading but refuses replacement of the destination.
    public static async Task RunCompletionSaveFailure(SceneTree tree)
    {
        await SaveFailureThenRetry(tree);
        await SaveFailureThenCancel(tree, mappedLegacy: false);
        await SaveFailureThenCancel(tree, mappedLegacy: true);
    }

    private static async Task SaveFailureThenRetry(SceneTree tree)
    {
        IsolatedPrologueFixture.ValidateStorageRoot();
        await using var f = await Fixture.Create(tree, "save-replace-denied", true);
        f.AssertRestored();
        f.Place(new Vector2(7200,1080));
        await f.Settle();
        byte[] before = File.ReadAllBytes(f.Paths.SavePath);
        ulong playerId = f.Level.Player.GetInstanceId();
        using (var saveLock = new FileStream(f.Paths.SavePath, FileMode.Open, System.IO.FileAccess.Read, FileShare.Read))
        {
            Send(InputActions.MoveRight, true);
            try
            {
                for (int frame = 0; frame < 90 && f.Level.Player.GlobalPosition.X - Radius <= DoorRight; frame++)
                    await Frames(tree, 1);
            }
            finally { Send(InputActions.MoveRight, false); }
            await Frames(tree, 8);
            f.Observe("save-replace-denied");
            TestAssert.True(f.Level.Player.GlobalPosition.X - Radius > DoorRight && f.Supported(),
                "locked-save fixture really crossed the open exit on authored floor");
            TestAssert.True(before.SequenceEqual(File.ReadAllBytes(f.Paths.SavePath)) && !f.Saved.Completed,
                "locked completion keeps original durable save bytes");
            TestAssert.True(f.Completion.CompletionCount == 0 && f.CompletionEvents == 0 &&
                !f.Flow.Save.Completed && f.Flow.State != PrologueGameFlowState.Completed,
                "failed completion save must not publish success or mutate live completion");
            f.AssertPendingPanel();
            var dialog = f.Flow.GetNode<ConfirmationDialog>("CompletionSaveFailure");
            TestAssert.True(dialog.Visible && f.Flow.State == PrologueGameFlowState.CompletionSaveFailed &&
                !f.Level.Player.IsPhysicsProcessing(), "completion failure freezes gameplay and offers retry");
            TestAssert.True(tree.Paused, "completion failure pauses the world while awaiting an explicit decision");
            await Capture(tree, f.Paths.CaseRoot, "01-save-failure");
            Vector2 stopped = f.Level.Player.GlobalPosition;
            Send(InputActions.MoveRight, true);
            await Frames(tree, 60);
            Send(InputActions.MoveRight, false);
            TestAssert.True(f.Level.Player.GlobalPosition == stopped && f.Health.CurrentHealth > 0,
                "held movement cannot walk failed-save player out of the world");
            Send("ui_accept", true);
            dialog.GetOkButton().EmitSignal(Button.SignalName.Pressed);
            await Frames(tree, 3);
            TestAssert.True(dialog.Visible && f.CompletionEvents == 0 && !f.Flow.Save.Completed &&
                before.SequenceEqual(File.ReadAllBytes(f.Paths.SavePath)),
                "retry while locked stays pending without a false success");
        }
        await Frames(tree, 15);
        var retryDialog = f.Flow.GetNode<ConfirmationDialog>("CompletionSaveFailure");
        for (int repeat = 0; repeat < 4; repeat++)
        {
            retryDialog.PushInput(new InputEventKey { Keycode = Key.Enter, PhysicalKeycode = Key.Enter,
                Pressed = true, Echo = true });
            retryDialog.EmitSignal(AcceptDialog.SignalName.Confirmed);
            await Frames(tree, 1);
        }
        TestAssert.True(!f.Saved.Completed && f.Completion.CompletionCount == 0,
            "held accept and echo cannot retry after unlock without a new release");
        Send("ui_accept", false);
        await Frames(tree, 2);
        f.Flow.GetNode<ConfirmationDialog>("CompletionSaveFailure").GetOkButton()
            .EmitSignal(Button.SignalName.Pressed);
        await Frames(tree, 3);
        TestAssert.True(f.Saved.Completed && f.Flow.Save.Completed &&
            !tree.Paused && f.Flow.State == PrologueGameFlowState.Completed && f.Completion.CompletionCount == 1 &&
            f.CompletionEvents == 1 && !f.Level.Player.IsPhysicsProcessing() &&
            f.Level.Player.GetInstanceId() == playerId &&
            f.Level.GetNode<Control>("Hud/FormalHud/Completion").IsVisibleInTree() &&
            !f.Flow.GetNode<ConfirmationDialog>("CompletionSaveFailure").Visible,
            "explicit unlocked retry saves and completes the same frozen actor exactly once");
        byte[] committed = File.ReadAllBytes(f.Paths.SavePath);
        f.Flow.GetNode<ConfirmationDialog>("CompletionSaveFailure").EmitSignal(AcceptDialog.SignalName.Confirmed);
        f.Flow.GetNode<ConfirmationDialog>("CompletionSaveFailure").EmitSignal(AcceptDialog.SignalName.Canceled);
        await Frames(tree, 4);
        TestAssert.True(f.Flow.State == PrologueGameFlowState.Completed && f.CompletionEvents == 1 &&
            committed.SequenceEqual(File.ReadAllBytes(f.Paths.SavePath)),
            "late dialog signals cannot duplicate completion or cancel the successful terminal state");
        await Capture(tree, f.Paths.CaseRoot, "02-retry-complete");
        GD.Print("COMPLETION_SAVE_FAILURE_OK controlledFixture=true osKeyboard=false fullRoute=false");
    }

    private static async Task SaveFailureThenCancel(SceneTree tree, bool mappedLegacy)
    {
        await using var f = await Fixture.Create(tree, "cancel-" + mappedLegacy, true, sixFragments: true);
        f.AssertRestored();
        f.Place(new Vector2(7200,1080));
        await f.Settle();
        byte[] saved = File.ReadAllBytes(f.Paths.SavePath);
        using (var saveLock = new FileStream(f.Paths.SavePath, FileMode.Open, System.IO.FileAccess.Read,
            mappedLegacy ? FileShare.None : FileShare.Read))
        {
            if (mappedLegacy) f.Completion.RestoreSession(PrologueObjective.Complete, completeMappedLegacyBossResult: true);
            else Send(InputActions.MoveRight, true);
            try
            {
                for (int frame = 0; frame < 90 && f.Flow.State == PrologueGameFlowState.Playing; frame++)
                    await Frames(tree, 1);
            }
            finally { Send(InputActions.MoveRight, false); }
            var dialog = f.Flow.GetNode<ConfirmationDialog>("CompletionSaveFailure");
            TestAssert.True(tree.Paused && f.CompletionEvents == 0 && dialog.Visible,
                "current and explicit mapped-result failures enter pending modal");
            if (mappedLegacy) TestAssert.True(f.Level.Player.GlobalPosition.X + Radius < DoorRight,
                "mapped legacy failure does not require a physical exit crossing");
            dialog.GetCancelButton().EmitSignal(Button.SignalName.Pressed);
            await Frames(tree, 3);
            TestAssert.True(!tree.Paused && !dialog.Visible && f.Flow.State == PrologueGameFlowState.Title &&
                f.Flow.HasSave && f.Flow.Save.BossDefeated && f.Flow.Save.CollectedFragmentIds.Count == 6 &&
                !f.Flow.Save.Completed,
                "cancel pending completion preserves last saved boss and six fragments at Title");
        }
        TestAssert.True(saved.SequenceEqual(File.ReadAllBytes(f.Paths.SavePath)),
            "read-denied or replace-denied cancellation leaves the original disk bytes unchanged");
        f.Level.GetNode<Button>("ProductUi/MainMenu/Panel/Buttons/Continue").EmitSignal(Button.SignalName.Pressed);
        await Frames(tree, 5);
        TestAssert.True(f.Flow.State == PrologueGameFlowState.Playing && f.Level.Player.IsPhysicsProcessing() &&
            f.Level.Progress.BossDefeated && f.Level.Progress.CollectedFragmentIds.Count == 6 &&
            f.Completion.CompletionCount == 0 && !f.Saved.Completed && !tree.Paused,
            "Continue after cancellation restores saved defeated Boss and fragments without false completion");
        GD.Print($"COMPLETION_CANCEL_OK mappedSessionException={mappedLegacy} savedFragments=6 boss=true completed=false");
    }

    private static async Task Capture(SceneTree tree, string directory, string name)
    {
        if (DisplayServer.GetName() == "headless") return;
        await Frames(tree, 2);
        await tree.ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        using Image image = tree.Root.GetTexture().GetImage();
        TestAssert.Equal(Error.Ok, image.SavePng(Path.Combine(directory, name + ".png")), "completion proof image saved");
        await Frames(tree, 90);
    }

    public static async Task RunPhysicalExitRegression(SceneTree tree)
    {
        IsolatedPrologueFixture.ValidateStorageRoot();
        await BossDeathWaitsForExit(tree);
        await Negative(tree, "left", new Vector2(7200,1080));
        await Negative(tree, "airborne", new Vector2(7400,900));
        await Negative(tree, "unsupported", new Vector2(7750,1080));
        await Negative(tree, "dead", new Vector2(7400,1080));
        await SupportedInputExitAndTerminalPolicy(tree);
    }

    public static async Task Run(SceneTree tree)
    {
        string storage = ValidateLaunch();
        await AwaitIdentityRelease(tree, storage);
        GD.Print("NORMAL_EXIT_SCOPE controlledSavePoseHealth=true normalLessons=false fullCombat=false fullRoute=false headlessFixedFps=true");
        await BossDeathWaitsForExit(tree);
        await Negative(tree, "left", new Vector2(7200,1080));
        await Negative(tree, "airborne", new Vector2(7400,900));
        await Negative(tree, "unsupported", new Vector2(7750,1080));
        await Negative(tree, "dead", new Vector2(7400,1080));
        await SupportedInputExitAndTerminalPolicy(tree);
    }

    // Break caught: normal BossDefeated completes without the whole alive capsule exiting.
    private static async Task BossDeathWaitsForExit(SceneTree tree)
    {
        await using var f = await Fixture.Create(tree, "fresh-boss", false);
        TestAssert.True(f.Flow.State == PrologueGameFlowState.Playing && f.SessionStarts == 1,
            "fresh fixture normal Title Start enters one Playing session");
        // Public, explicitly controlled checkpoint/objective prerequisite AFTER normal Start.
        f.Level.RestoreFromSave(Checkpoint(false));
        f.Completion.RestoreSession(PrologueObjective.DefeatBellkeeper);
        f.Place(new Vector2(7200,1080));
        await f.Settle();
        var arena = f.Level.CurrentSectorRoot.GetNode<BossArenaController>("Gameplay/BossArenaController");
        int defeats = 0;
        arena.EncounterDefeated += () => defeats++;
        arena.BeginEncounter();
        TestAssert.True(arena.IsEncounterActive && GodotObject.IsInstanceValid(arena.CurrentBoss),
            "fresh fixture actual BossArena public encounter lifecycle is active");
        var bossHealth = arena.CurrentBoss.GetNode<HealthComponent>("Health");
        TestAssert.True(f.Completion.CurrentObjective == PrologueObjective.DefeatBellkeeper &&
            !f.Level.Progress.BossDefeated && f.Flow.State == PrologueGameFlowState.Playing &&
            f.Completion.CompletionCount == 0 && f.CompletionEvents == 0 && !f.Saved.Completed &&
            bossHealth.CurrentHealth > 0 && f.Level.Player.GlobalPosition.X + Radius < DoorRight,
            "fresh fixture valid pre-death objective health save and left-of-door prerequisites");
        TestAssert.True(bossHealth.ApplyDamage(new DamageRequest(bossHealth.CurrentHealth,
                Vector2.Right, 0, 970001UL)), "fresh fixture actual controlled BossHealth death accepted");
        await Frames(tree, 8);
        TestAssert.True(bossHealth.CurrentHealth == 0 && defeats == 1 &&
            f.Level.Progress.BossDefeated && f.Door.Disabled &&
            f.Level.Player.GlobalPosition.X + Radius < DoorRight && f.Health.CurrentHealth > 0,
            "fresh fixture death lifecycle opens exit exactly once with living player still left");
        GD.Print("NORMAL_EXIT_BOSS_PREREQUISITES_OK start=1 objectiveBefore=DefeatBellkeeper bossHealth=0 defeats=1 doorOpen=true playerLeft=true alive=true saveBefore=false");
        f.Observe("boss-after-death");
        TestAssert.True(f.Flow.State == PrologueGameFlowState.Playing &&
            f.Completion.CompletionCount == 0 && f.CompletionEvents == 0 && !f.Saved.Completed, Target);
        f.AssertPendingPanel();
        GD.Print("NORMAL_EXIT_CASE_OK name=boss-waits");
    }

    // Break caught independently: stale floor contact or an invalid pose/Health0 is accepted.
    private static async Task Negative(SceneTree tree, string name, Vector2 pose)
    {
        await using var f = await Fixture.Create(tree, name, true, zeroHealth: name == "dead");
        f.AssertRestored();
        f.Place(pose); // one initial fixture relocation only; never refreshed per frame.
        if (name == "left") await f.Settle();
        else await Frames(tree, 3);
        TestAssert.True(f.Level.Player.GlobalPosition.X > DoorRight + Radius || name == "left",
            $"{name} fixture is on intended independent horizontal side");
        if (name == "airborne" || name == "unsupported")
            TestAssert.True(!f.Supported(), $"{name} fixture lacks actual ArenaFloor support");
        if (name == "unsupported")
            TestAssert.True(f.Level.Player.GlobalPosition.X - Radius > FloorRight,
                "unsupported fixture entire capsule beyond authored floor");
        if (name == "dead")
            TestAssert.True(f.Health.CurrentHealth == 0 && f.Supported(),
                "dead fixture pre-mounted MaxHealth0 has actual ArenaFloor support without recovery/healing");
        for (int frame = 0; frame < 12; frame++)
        {
            f.AssertPendingPanel();
            TestAssert.True(f.Flow.State != PrologueGameFlowState.Completed &&
                f.Completion.CompletionCount == 0 && f.CompletionEvents == 0 && !f.Saved.Completed,
                $"normal {name} pose must not trigger physical exit completion");
            await Frames(tree, 1);
        }
        f.Observe(name);
        GD.Print($"NORMAL_EXIT_CASE_OK name={name}");
    }

    // Break caught: restored Complete prefix lacks a supported-exit watcher / terminal policy.
    private static async Task SupportedInputExitAndTerminalPolicy(SceneTree tree)
    {
        await using var f = await Fixture.Create(tree, "supported-input", true);
        f.AssertRestored();
        f.Place(new Vector2(7200,1080));
        await f.Settle();
        ulong playerId = f.Level.Player.GetInstanceId(), levelId = f.Level.GetInstanceId();
        int starts = f.SessionStarts;
        Send(InputActions.MoveRight, true);
        try
        {
            for (int frame = 0; frame < 180 && f.Flow.State == PrologueGameFlowState.Playing; frame++)
            {
                await Frames(tree, 1);
                if (f.Flow.State == PrologueGameFlowState.Completed)
                    TestAssert.True(f.Level.Player.GlobalPosition.X - Radius > DoorRight &&
                        f.Level.Player.GlobalPosition.X + Radius <= FloorRight &&
                        f.Health.CurrentHealth > 0 && f.Supported(),
                        "normal completion occurs only with entire alive capsule beyond door on actual ArenaFloor");
            }
        }
        finally { Send(InputActions.MoveRight, false); }
        f.Observe("supported-input");
        TestAssert.True(f.Flow.State == PrologueGameFlowState.Completed &&
            f.Completion.CompletionCount == 1 && f.CompletionEvents == 1 && f.Saved.Completed,
            "restored normal Complete prefix must complete exactly once after real supported rightward input");
        TestAssert.True(f.Level.Player.GetInstanceId() == playerId && f.Level.GetInstanceId() == levelId &&
            f.SessionStarts == starts && starts == 1, "normal input exit keeps same player level and session");
        GD.Print("NORMAL_EXIT_CASE_OK name=supported-input");

        var panel = f.Level.GetNode<Control>("Hud/FormalHud/Completion");
        string title = panel.GetNode<Label>("Panel/Title").Text;
        TestAssert.True(panel.IsVisibleInTree() && !title.Contains("样片") &&
            !title.Contains("SAMPLE", StringComparison.OrdinalIgnoreCase) &&
            (title.Contains("序章") || title.Contains("PROLOGUE", StringComparison.OrdinalIgnoreCase)),
            "normal terminal policy keeps ordinary completion panel visible, never sample title");
        Vector2 terminalPose = f.Level.Player.GlobalPosition;
        Send(InputActions.MoveRight, true);
        try
        {
            for (int frame = 0; frame < 120; frame++)
            {
                await Frames(tree, 1);
                TestAssert.True(f.Level.Player.GlobalPosition.DistanceTo(terminalPose) < 1f &&
                    f.Level.Player.GlobalPosition.X + Radius <= FloorRight &&
                    !f.Level.Player.IsPhysicsProcessing(),
                    "normal terminal input cannot walk player out of authored world");
            }
        }
        finally { Send(InputActions.MoveRight, false); }
        TestAssert.True(f.Level.Player.GlobalPosition.DistanceTo(terminalPose) < 1f &&
            f.Level.Player.GlobalPosition.X + Radius <= FloorRight &&
            !f.Level.Player.IsPhysicsProcessing(),
            "normal terminal input cannot walk player out of authored world");
        int recoveryEvents = 0;
        f.Level.RecoveryRequested += _ => recoveryEvents++;
        using (var expectedTerminalWarning = new ExpectedWarningLogger())
        {
            f.Level.RequestRespawn("FOCUSED TERMINAL RECOVERY");
            await Frames(tree, 8);
            expectedTerminalWarning.AssertExact(new ExpectedWarningLogger.Expected(
                "Rejected prologue flow transition Completed->Recovering.",
                "scripts/prologue/PrologueGameFlowController.cs", "Reject", 1));
        }
        GD.Print("NORMAL_EXIT_EXPECTED_TERMINAL_WARNING_OK count=1 source=scripts/prologue/PrologueGameFlowController.cs function=Reject");
        TestAssert.True(f.Flow.State == PrologueGameFlowState.Completed && recoveryEvents == 0 &&
            !f.Level.Player.IsPhysicsProcessing() && panel.IsVisibleInTree() &&
            f.Level.Player.GetInstanceId() == playerId && f.Level.GetInstanceId() == levelId &&
            f.SessionStarts == starts && f.Completion.CompletionCount == 1 &&
            f.CompletionEvents == 1 && f.Saved.Completed,
            "normal terminal recovery cannot reopen gameplay or duplicate completion");
        f.Observe("terminal");
        GD.Print("NORMAL_EXIT_CASE_OK name=terminal");
    }

    private sealed class Fixture : IAsyncDisposable
    {
        public PrologueLevelController Level;
        public PrologueGameFlowController Flow;
        public PrologueCompletionController Completion;
        public IsolatedPrologueFixturePaths Paths;
        public int CompletionEvents, SessionStarts;
        private SceneTree _tree;
        public HealthComponent Health => Level.Player.GetNode<HealthComponent>("Health");
        public CollisionShape2D Door => Level.CurrentSectorRoot.GetNode<CollisionShape2D>("Gameplay/Doors/Exit/Collision");
        public PrologueSaveData Saved => PrologueSaveStore.LoadOrNew(Paths.SavePath);

        public static async Task<Fixture> Create(SceneTree tree, string name, bool continuing,
            bool zeroHealth = false, bool sixFragments = false)
        {
            var f = new Fixture { _tree = tree };
            f.Level = GD.Load<PackedScene>("res://scenes/prologue/PrologueLevel.tscn")
                .Instantiate<PrologueLevelController>();
            try
            {
                f.Level.Name = "NormalExit_" + name;
                f.Paths = IsolatedPrologueFixture.Configure(f.Level, "normal-exit-" + name);
                f.Flow = f.Level.GetNode<PrologueGameFlowController>("GameFlow");
                f.Completion = f.Level.GetNode<PrologueCompletionController>("Completion");
                f.Flow.HeadlessAutoStart = false;
                f.Flow.AutomationAutoStart = false;
                TestAssert.True(!f.Flow.BasinQualitySampleMode && !f.Completion.BasinQualitySampleMode,
                    "fixture is generated normal level not development sample");
                if (zeroHealth) f.Level.GetNode<HealthComponent>("PlayerRoot/Player/Health").MaxHealth = 0;
                if (continuing)
                {
                    var save = Checkpoint(true);
                    if (sixFragments) BossAdmissionFixturePrerequisites.Apply(save);
                    PrologueSaveStore.SaveAtomic(f.Paths.SavePath, save);
                }
                f.Flow.SessionStarted += () => f.SessionStarts++;
                f.Completion.PrologueCompleted += () => f.CompletionEvents++;
                tree.Root.AddChild(f.Level);
                await Frames(tree, 3);
                TestAssert.True(f.Level.Player.GetNode<CollisionShape2D>("BodyCollider").Shape is
                    CapsuleShape2D capsule && capsule.Radius == 20 && capsule.Height == 72,
                    "fixture uses actual authored radius20 height72 BodyCollider");
                TestAssert.True(f.Flow.State == PrologueGameFlowState.Title &&
                    f.Flow.HasSave == continuing, "fixture Title and Continue eligibility match retained save");
                f.Level.GetNode<Button>("ProductUi/MainMenu/Panel/Buttons/" +
                    (continuing ? "Continue" : "Start")).EmitSignal(Button.SignalName.Pressed);
                await Frames(tree, 2);
                TestAssert.True(f.Flow.State == PrologueGameFlowState.Playing && f.SessionStarts == 1,
                    "fixture explicit Title button starts exactly one Playing session");
                GD.Print($"NORMAL_EXIT_FIXTURE name={name} storage={f.Paths.CaseRoot} continue={continuing} health0={zeroHealth}");
                return f;
            }
            catch { await f.DisposeAsync(); throw; }
        }

        public void AssertRestored()
        {
            var save = Saved;
            TestAssert.True(save.Version == PrologueSaveData.CurrentVersion && save.LayoutVersion == 2 &&
                save.CheckpointId == "canyon.v2.temple-forecourt" &&
                save.CheckpointWorldPosition == new Vector2(5920,1080) &&
                save.BossDefeated && !save.Completed && save.Objective == "Complete" &&
                Level.CurrentSector == PrologueSectorId.LostTemple && Level.Progress.BossDefeated &&
                Completion.CurrentObjective == PrologueObjective.Complete && Completion.CompletionCount == 0 &&
                CompletionEvents == 0 && Flow.State == PrologueGameFlowState.Playing && Door.Disabled,
                "restored fixture current schema layout2 checkpoint boss objective and uncompleted Continue prerequisites");
            AssertPendingPanel();
        }
        public void AssertPendingPanel() => TestAssert.True(
            !Level.GetNode<Control>("Hud/FormalHud/Completion").IsVisibleInTree(),
            "normal pending physical exit cannot display a full completion panel before exit");
        public void Place(Vector2 pose)
        {
            ReleaseInputs();
            Level.Player.GlobalPosition = pose;
            Level.Player.Velocity = Vector2.Zero;
        }
        public async Task Settle()
        {
            var floorShape = Level.CurrentSectorRoot.GetNode<CollisionShape2D>("Gameplay/Collision/ArenaFloor/Collision");
            TestAssert.True(floorShape.Shape is RectangleShape2D floorRectangle &&
                floorShape.GlobalPosition.X - floorRectangle.Size.X / 2 == FloorLeft &&
                floorShape.GlobalPosition.X + floorRectangle.Size.X / 2 == FloorRight &&
                floorShape.GlobalPosition.Y - floorRectangle.Size.Y / 2 == FloorTop &&
                Door.Shape is RectangleShape2D doorRectangle &&
                Door.GlobalPosition.X + doorRectangle.Size.X / 2 == DoorRight,
                "fixture actual authored floor and door match independently derived literal geometry");
            for (int frame = 0; frame < 60 && !Supported(); frame++) await Frames(_tree, 1);
            TestAssert.True(Health.CurrentHealth > 0 && Level.CurrentSector == PrologueSectorId.LostTemple &&
                Level.Player.IsPhysicsProcessing() && Supported() &&
                MathF.Abs(Level.Player.GlobalPosition.Y + 36 - FloorTop) < 2f,
                "fixture settles alive on actual upward ArenaFloor slide contact at authored top1116");
        }
        public bool Supported()
        {
            var player = Level.Player;
            if (!player.IsOnFloor() || player.GlobalPosition.X - Radius < FloorLeft ||
                player.GlobalPosition.X + Radius > FloorRight) return false;
            var floor = Level.CurrentSectorRoot.GetNode<StaticBody2D>("Gameplay/Collision/ArenaFloor");
            for (int i = 0; i < player.GetSlideCollisionCount(); i++)
            {
                var collision = player.GetSlideCollision(i);
                if (collision.GetCollider() == floor && collision.GetNormal().Dot(Vector2.Up) > .7f) return true;
            }
            return false;
        }
        public void Observe(string name) => GD.Print($"NORMAL_EXIT_OBSERVATION name={name} " +
            $"position={Level.Player.GlobalPosition} velocity={Level.Player.Velocity} floor={Level.Player.IsOnFloor()} " +
            $"arenaContact={Supported()} health={Health.CurrentHealth} state={Flow.State} " +
            $"count={Completion.CompletionCount} signals={CompletionEvents} savedCompleted={Saved.Completed} starts={SessionStarts}");
        public async ValueTask DisposeAsync()
        {
            ReleaseInputs();
            _tree.Paused = false;
            if (!GodotObject.IsInstanceValid(Level)) return;
            if (Level.IsInsideTree())
            {
                Level.SetGameplayActive(false);
                await TestSceneTeardown.FreeSceneAfterAudioDrain(_tree, Level);
            }
            else Level.Free();
        }
    }

    private static PrologueSaveData Checkpoint(bool defeated)
    {
        var saved = new PrologueSaveData
        {
            Version = PrologueSaveData.CurrentVersion, LayoutVersion = 2, CheckpointId = "canyon.v2.temple-forecourt",
            HasActivatedCheckpoint = true, CheckpointSector = "LostTemple",
            CheckpointWorldX = 5920, CheckpointWorldY = 1080, BossDefeated = defeated,
            Objective = defeated ? "Complete" : "DefeatBellkeeper", Completed = false
        };
        // Only the controlled fresh fight needs admission inputs. Defeated
        // Continue negatives intentionally retain zero fragments/no seal/bell.
        if (!defeated) BossAdmissionFixturePrerequisites.Apply(saved);
        return saved;
    }

    private static string ValidateLaunch()
    {
        string[] args = OS.GetCmdlineUserArgs();
        const string prefix = "--case-storage-root=";
        if (args.Length != 2 || args.Count(a => a == "--physical-exit-only") != 1 ||
            args.Count(a => a.StartsWith(prefix, StringComparison.Ordinal)) != 1 ||
            OS.HasEnvironment("KNIGHT_PLAYTEST_SOURCE"))
            throw new ArgumentException("focused exit requires exact isolated arguments and no hands-on grants");
        string raw = args.Single(a => a.StartsWith(prefix, StringComparison.Ordinal))[prefix.Length..];
        if (!Path.IsPathFullyQualified(raw)) throw new ArgumentException("focused raw storage must be fully absolute");
        string storage = Trim(Path.GetFullPath(raw));
        string caseRoot = Path.GetDirectoryName(storage)!;
        string expectedTemp = Path.Combine(caseRoot, "unit-temp");
        if (!string.Equals(Path.GetDirectoryName(caseRoot), Trim(Path.GetFullPath(
                "E:/OurGame/Knight/.evidence/september09-continuous-canyon/task10")), StringComparison.OrdinalIgnoreCase) ||
            Path.GetFileName(storage) != "storage" || !Directory.Exists(storage) ||
            Directory.EnumerateFileSystemEntries(storage).Any() || !Directory.Exists(expectedTemp) ||
            !string.Equals(Trim(Path.GetFullPath(ProjectSettings.GlobalizePath("res://"))), Trim(Path.GetFullPath(
                "E:/OurGame/Knight/.worktrees/task6-certified-final-20260902T0555")), StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("focused external case storage must be empty and in exact linked checkout");
        foreach (string name in new[] { "TMP", "TEMP" })
        {
            string value = System.Environment.GetEnvironmentVariable(name);
            if (value == null || !Path.IsPathFullyQualified(value) ||
                !string.Equals(Trim(Path.GetFullPath(value)), expectedTemp, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("focused child TMP/TEMP must be case-local");
        }
        if (!string.Equals(Trim(Path.GetFullPath(Path.GetTempPath())), expectedTemp, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("focused runtime temporary path must be case-local");
        foreach (string path in new[] { storage, expectedTemp }) AssertNoReparse(path);
        return storage;
    }
    private static async Task AwaitIdentityRelease(SceneTree tree, string storage)
    {
        string release = Path.Combine(Path.GetDirectoryName(storage)!, "tests-identity.release");
        string pid = OS.GetProcessId().ToString(CultureInfo.InvariantCulture);
        var timer = Stopwatch.StartNew();
        while (timer.Elapsed < TimeSpan.FromSeconds(10))
        {
            if (File.Exists(release))
            {
                AssertNoReparse(release);
                if (File.ReadAllText(release) != pid) throw new InvalidOperationException("focused identity release PID mismatch");
                GD.Print("NORMAL_EXIT_IDENTITY_RELEASED pid=" + pid);
                return;
            }
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        }
        throw new TimeoutException("focused actual PID identity release missing before mount");
    }
    private static void AssertNoReparse(string path)
    {
        if (File.Exists(path) && (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
            throw new ArgumentException("focused release marker rejects reparse");
        for (DirectoryInfo current = new(Directory.Exists(path) ? path : Path.GetDirectoryName(path)!);
             current != null; current = current.Parent)
            if (current.Exists && (current.Attributes & FileAttributes.ReparsePoint) != 0)
                throw new ArgumentException("focused case rejects reparse ancestors");
    }
    private static string Trim(string path) => path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    private static void Send(StringName action, bool down)
    {
        Input.ParseInputEvent(new InputEventAction { Action = action, Pressed = down, Strength = down ? 1f : 0f });
        Input.FlushBufferedEvents();
    }
    private static void ReleaseInputs()
    {
        Send("ui_accept", false);
        foreach (var action in new[] { InputActions.MoveLeft, InputActions.MoveRight, InputActions.MoveUp,
            InputActions.MoveDown, InputActions.PrimaryAction, InputActions.Shield, InputActions.Jump,
            InputActions.Dash, InputActions.SelectAttackMode, InputActions.SelectGrappleMode }) Send(action, false);
    }
    private static async Task Frames(SceneTree tree, int count)
    {
        for (int i = 0; i < count; i++)
        {
            await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        }
    }
}
