using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Knight.Prologue;
using Knight.Prologue.Seamless;

namespace Knight.Tests.Integration;

internal readonly record struct Task9GeneratedMainContext(
    PrologueLevelController Level,
    CharacterBody2D Player,
    SeamlessPrologueWorld World,
    Func<int, Task> AdvancePhysics);

internal interface ITask9GeneratedMainMilestoneObserver
{
    void Configure(PrologueLevelController level);
    Task BeforeBasin(Task9GeneratedMainContext context);
    Task AtDescent(Task9GeneratedMainContext context);
    Task AfterDescentGrapple(Task9GeneratedMainContext context);
    Task BeforeBasinSeam(Task9GeneratedMainContext context);
    Task AtBasinBeforeShieldLane(Task9GeneratedMainContext context);
    Task AfterBasinCheckpoint(Task9GeneratedMainContext context);
    Task AfterTempleCheckpoint(Task9GeneratedMainContext context);
    Task AfterBossTrigger(Task9GeneratedMainContext context);
    Task AfterBossRetry(Task9GeneratedMainContext context);
}

public static class PrologueTask9GeneratedMainTests
{
    public static async Task Run(SceneTree tree, bool captureFrames)
    {
        var observer = new RecoveryObserver();
        await PrologueCanyonGeneratedMainRouteTests.Run(tree, captureFrames, observer);
        observer.AssertClosedTelemetry();
    }

    private sealed class RecoveryObserver : ITask9GeneratedMainMilestoneObserver
    {
        private readonly string _outputDirectory =
            $"user://task9-automated-generated-main/{Guid.NewGuid():N}";
        private string _latestJsonPath = string.Empty;
        private string _latestMarkdownPath = string.Empty;
        private PrologueLevelController _level;
        private PrologueGameFlowController _flow;
        private PrologueCompletionController _completion;
        private readonly ProloguePlaytestSession _session = new(
            "task9-automated-route", "2026-08-27T00:00:00Z", "AutomatedFixture");
        private readonly Dictionary<string, int> _persistRequests = new(StringComparer.Ordinal);
        private bool _bossDefeatPersistedBeforeCompletion;
        private bool _shieldObjectiveWaitedForNewEdge;

        public void Configure(PrologueLevelController level)
        {
            _level = level;
            _flow = level.GetNode<PrologueGameFlowController>("GameFlow");
            _completion = level.GetNode<PrologueCompletionController>("Completion");
            ProloguePlaytestRecorder recorder = level.GetNode<ProloguePlaytestRecorder>("PlaytestRecorder");
            recorder.OutputDirectory = _outputDirectory;
            recorder.FlushIntervalSeconds = .25d;
            _latestJsonPath = $"{_outputDirectory}/latest.json";
            _latestMarkdownPath = $"{_outputDirectory}/latest.md";
            level.ProgressPersistRequested += OnProgressPersistRequested;
        }

        public async Task BeforeBasin(Task9GeneratedMainContext context)
        {
            // Re-add after GameFlow._Ready so this observer sees the on-disk result of the
            // single synchronous persistence handler rather than racing it.
            _level.ProgressPersistRequested -= OnProgressPersistRequested;
            _level.ProgressPersistRequested += OnProgressPersistRequested;
            _session.VisitSector(context.Level.CurrentSector!.Value);
            SendAction(Knight.Shared.InputActions.SelectAttackMode, true);
            await context.AdvancePhysics(2);
            SendAction(Knight.Shared.InputActions.SelectAttackMode, false);
            TestAssert.Equal(PrologueObjective.LearnGrappleMode,
                context.Level.GetNode<PrologueCompletionController>("Completion").CurrentObjective,
                "Q advances exactly one objective edge before the descent lesson");
            _session.RecordModeSwitch(context.Player
                .GetNode<Knight.Player.PlayerActionModeController>("ActionModes").CurrentMode);
            foreach (StringName slot in new[]
                     { Knight.Shared.InputActions.Slot2, Knight.Shared.InputActions.Slot3,
                         Knight.Shared.InputActions.Slot4, Knight.Shared.InputActions.Slot1 })
            {
                SendAction(slot, true);
                await context.AdvancePhysics(2);
                SendAction(slot, false);
                _session.RecordSlotSelection(context.Player
                    .GetNode<Knight.Player.PlayerActionModeController>("ActionModes").ActiveSlot);
            }
            await VerifyPhysicalDeath(context, PrologueSectorId.CanyonEntrance,
                expectedActivated: false, "before-basin");
        }

        public async Task AtDescent(Task9GeneratedMainContext context)
        {
            _session.VisitSector(context.Level.CurrentSector!.Value);
            SendAction(Knight.Shared.InputActions.SelectGrappleMode, true);
            await context.AdvancePhysics(2);
            SendAction(Knight.Shared.InputActions.SelectGrappleMode, false);
            TestAssert.Equal(PrologueObjective.ReachBasin,
                context.Level.GetNode<PrologueCompletionController>("Completion").CurrentObjective,
                "E advances exactly one objective edge in the mounted Descent sector");
            _session.RecordModeSwitch(context.Player
                .GetNode<Knight.Player.PlayerActionModeController>("ActionModes").CurrentMode);
        }

        public async Task AfterDescentGrapple(Task9GeneratedMainContext context)
        {
            Knight.Abilities.GrappleSurface2D ice = context.Level.CurrentSectorRoot
                .GetNode<Knight.Abilities.GrappleSurface2D>(
                    "Gameplay/GrappleSurfaces/VisibleIceRejection");
            Knight.Abilities.MechanicalClawController claw = context.Player
                .GetNode<Knight.Abilities.MechanicalClawController>("MechanicalClaw");
            SendAction(Knight.Shared.InputActions.SelectGrappleMode, true);
            await context.AdvancePhysics(1);
            SendAction(Knight.Shared.InputActions.SelectGrappleMode, false);
            SendMouse(context.Player, MouseButton.Left, true, ice.WorldAnchor);
            for (int frame = 0; frame < 30 &&
                 claw.CurrentFrame.Failure != Knight.Abilities.MechanicalClawFailure.IceSlip; frame++)
            {
                MoveMouse(context.Player, ice.WorldAnchor);
                await context.AdvancePhysics(1);
            }
            SendMouse(context.Player, MouseButton.Left, false, ice.WorldAnchor);
            TestAssert.Equal(Knight.Abilities.MechanicalClawFailure.IceSlip,
                claw.CurrentFrame.Failure,
                "real generated-main telemetry route records the authored Ice rejection");
            _session.RecordGrappleFailure(claw.CurrentFrame.Failure);
        }

        public async Task BeforeBasinSeam(Task9GeneratedMainContext context)
        {
            // Create and release a real shield edge while ReachBasin is still current. The
            // residual use/heat must not be mistaken for a new LearnShield rising edge.
            TestAssert.Equal(PrologueObjective.ReachBasin,
                context.Level.GetNode<PrologueCompletionController>("Completion").CurrentObjective,
                "pre-seam shield edge occurs before LearnShield becomes current");
            SendMouse(context.Player, MouseButton.Right, true,
                context.Player.GlobalPosition + Vector2.Right * 240f);
            await context.AdvancePhysics(2);
            TestAssert.True(context.Player.GetNode<Knight.Player.PlayerShieldController>("Shield").IsActive,
                "pre-Basin stale-edge fixture uses the real RMB/InputMap shield path");
            SendMouse(context.Player, MouseButton.Right, false,
                context.Player.GlobalPosition + Vector2.Right * 240f);
            await context.AdvancePhysics(1);
        }

        public Task AtBasinBeforeShieldLane(Task9GeneratedMainContext context)
        {
            _shieldObjectiveWaitedForNewEdge =
                context.Level.GetNode<PrologueCompletionController>("Completion").CurrentObjective ==
                PrologueObjective.LearnShield;
            return Task.CompletedTask;
        }

        public async Task AfterBasinCheckpoint(Task9GeneratedMainContext context)
        {
            _session.VisitSector(context.Level.CurrentSector!.Value);
            PrologueCheckpointSnapshot basin = context.Level.Progress.RestoreCheckpoint();
            _session.ActivateCheckpoint(basin.SectorId);
            Knight.Player.PlayerEnergyController energy = context.Player
                .GetNode<Knight.Player.PlayerEnergyController>("Energy");
            TestAssert.Equal(PrologueObjective.ClimbCanyon,
                context.Level.GetNode<PrologueCompletionController>("Completion").CurrentObjective,
                "the route's new real RMB edge advances LearnShield exactly once");
            SendMouse(context.Player, MouseButton.Right, true,
                context.Player.GlobalPosition + Vector2.Right * 240f);
            for (int frame = 0; frame < 240 && !energy.IsOverheated; frame++)
                await context.AdvancePhysics(1);
            SendMouse(context.Player, MouseButton.Right, false,
                context.Player.GlobalPosition + Vector2.Right * 240f);
            TestAssert.True(energy.IsOverheated,
                "real generated-main telemetry route reaches one shield overheat");
            _session.RecordShieldOverheat();
            for (int frame = 0; frame < 300 && energy.IsOverheated; frame++)
                await context.AdvancePhysics(1);
            TestAssert.True(!energy.IsOverheated,
                "shield cools before the post-checkpoint physical death");
            await VerifyPhysicalDeath(context, PrologueSectorId.CanyonBasin,
                expectedActivated: true, "after-basin");
        }

        public Task AfterTempleCheckpoint(Task9GeneratedMainContext context)
        {
            TestAssert.True(context.World.MountedSectorIds.Contains(PrologueSectorId.CanyonAscent),
                "automated telemetry observes the physically traversed Ascent beside Temple");
            _session.VisitSector(PrologueSectorId.CanyonAscent);
            _session.VisitSector(context.Level.CurrentSector!.Value);
            _session.ActivateCheckpoint(context.Level.Progress.RestoreCheckpoint().SectorId);
            _session.ObserveDiscoveries(context.Level.Progress.BellChainShortcutOpen,
                context.Level.Progress.Discoveries);
            return VerifyPhysicalDeath(context, PrologueSectorId.LostTemple,
                expectedActivated: true, "after-temple");
        }

        public async Task AfterBossTrigger(Task9GeneratedMainContext context)
        {
            TestAssert.True(context.Level.Progress.BellChainShortcutOpen,
                "physical Boss arrival retains the opened Temple shortcut");
            _session.ObserveDiscoveries(context.Level.Progress.BellChainShortcutOpen,
                context.Level.Progress.Discoveries);
            _session.BeginBossAttempt();
            // Let the display recorder observe the first physical encounter before the death that
            // starts the retry. Real hands-on play naturally has this presentation interval.
            await context.AdvancePhysics(2);
            await VerifyPhysicalDeath(context, PrologueSectorId.LostTemple,
                expectedActivated: true, "boss-retry");
            TestAssert.True(!context.Level.CurrentSectorRoot.GetNode<Knight.Boss.BossArenaController>(
                    "Gameplay/BossArenaController").IsEncounterActive,
                "Boss death recovery resets the real encounter before physical retry");
        }

        public async Task AfterBossRetry(Task9GeneratedMainContext context)
        {
            Knight.Boss.BossArenaController arena =
                context.Level.CurrentSectorRoot.GetNode<Knight.Boss.BossArenaController>(
                    "Gameplay/BossArenaController");
            TestAssert.True(arena.IsEncounterActive,
                "physical movement from Temple checkpoint starts a fresh Boss retry");
            _session.Advance(.25d);
            _session.BeginBossAttempt();
            Knight.Combat.HealthComponent bossHealth =
                arena.CurrentBoss.GetNode<Knight.Combat.HealthComponent>("Health");
            TestAssert.True(bossHealth.ApplyDamage(new Knight.Combat.DamageRequest(
                    bossHealth.CurrentHealth - 1, Vector2.Left, 0f, 990001UL)),
                "public Boss damage path prepares one-health final-strike verification");
            SendAction(Knight.Shared.InputActions.SelectAttackMode, true);
            await context.AdvancePhysics(1);
            SendAction(Knight.Shared.InputActions.SelectAttackMode, false);
            for (int attempt = 0; attempt < 20 && bossHealth.CurrentHealth > 0; attempt++)
            {
                SendAction(Knight.Shared.InputActions.MoveRight, true);
                SendAction(Knight.Shared.InputActions.PrimaryAction, true);
                await context.AdvancePhysics(2);
                SendAction(Knight.Shared.InputActions.PrimaryAction, false);
                SendAction(Knight.Shared.InputActions.MoveRight, false);
                await context.AdvancePhysics(16);
            }
            SendAction(Knight.Shared.InputActions.PrimaryAction, false);
            SendAction(Knight.Shared.InputActions.MoveRight, false);
            await context.AdvancePhysics(3);
            TestAssert.True(bossHealth.CurrentHealth == 0 &&
                context.Level.Progress.BossDefeated &&
                context.Level.GetNode<PrologueCompletionController>("Completion").CurrentObjective ==
                    PrologueObjective.Complete,
                $"real InputMap player attacks complete the retried Boss and exact objective " +
                $"sequence; boss_health={bossHealth.CurrentHealth} " +
                $"player={context.Player.GlobalPosition} boss={arena.CurrentBoss.GlobalPosition} " +
                $"objective={context.Level.GetNode<PrologueCompletionController>("Completion").CurrentObjective}");
            _session.Complete(context.Level.Progress.BellChainShortcutOpen,
                context.Level.Progress.Discoveries);
            GD.Print("TASK9_GENERATED_MAIN_BOSS_RETRY_OK physical=True completion=True");
        }

        public void AssertClosedTelemetry()
        {
            string absoluteJson = ProjectSettings.GlobalizePath(_latestJsonPath);
            string absoluteMarkdown = ProjectSettings.GlobalizePath(_latestMarkdownPath);
            TestAssert.True(!File.Exists(absoluteJson) && !File.Exists(absoluteMarkdown),
                "automated generated-main route leaves the production HandsOn recorder silent");
            ProloguePlaytestReport report = _session.BuildReport(PrologueSectorId.LostTemple,
                PrologueObjective.Complete, "2026-08-27T00:01:00Z");
            TestAssert.Equal("AutomatedFixture", report.EvidenceSource,
                "automated generated-main telemetry remains test-owned and explicitly labelled");
            TestAssert.Equal(string.Join("|", Enum.GetNames<Knight.Prologue.Seamless.PrologueSectorId>()),
                string.Join("|", report.SectorsVisited),
                "runtime telemetry covers all five continuous sectors in order");
            TestAssert.Equal(nameof(PrologueSectorId.CanyonBasin), report.FirstCheckpoint,
                "runtime telemetry records Basin as the first explicit checkpoint");
            TestAssert.Equal("CanyonBasin|LostTemple", string.Join("|", report.CheckpointsActivated),
                "runtime telemetry records both explicit checkpoints");
            TestAssert.True(report.GrappleFailuresByReason.TryGetValue(
                    nameof(Knight.Abilities.MechanicalClawFailure.IceSlip), out int iceFailures) &&
                iceFailures >= 1,
                "runtime telemetry records at least one authored Ice failure");
            TestAssert.True(report.ShieldOverheats >= 1 &&
                report.ModeSwitches.Contains(nameof(Knight.Player.PlayerActionMode.Attack)) &&
                report.ModeSwitches.Contains(nameof(Knight.Player.PlayerActionMode.Grapple)) &&
                report.SelectedSlots.Contains(1) && report.SelectedSlots.Contains(2) &&
                report.SelectedSlots.Contains(3) && report.SelectedSlots.Contains(4),
                "runtime telemetry records overheat, both modes and slots 1-4");
            TestAssert.True(report.BossAttempts >= 2,
                $"runtime telemetry records the first Boss attempt and retry; actual={report.BossAttempts}");
            TestAssert.True(report.BossDeaths >= 1,
                $"runtime telemetry records the physical Boss-sector death; actual={report.BossDeaths}");
            TestAssert.True(report.BossReentrySeconds.Count >= 1,
                $"runtime telemetry records physical Boss reentry; actual={report.BossReentrySeconds.Count}");
            TestAssert.True(report.Completed && report.ShortcutOpen,
                $"runtime telemetry closes with completion and shortcut persistence; " +
                $"completed={report.Completed} shortcut={report.ShortcutOpen}");
            TestAssert.True(_shieldObjectiveWaitedForNewEdge &&
                _persistRequests.GetValueOrDefault("shortcut") == 1 &&
                _persistRequests.GetValueOrDefault("boss-defeated") == 1 &&
                _bossDefeatPersistedBeforeCompletion,
                $"stale pre-objective shield state is ignored and real Bell/Boss events request " +
                $"exactly one persistence write each; stale_shield_ignored={_shieldObjectiveWaitedForNewEdge} " +
                $"shortcut={_persistRequests.GetValueOrDefault("shortcut")} " +
                $"boss={_persistRequests.GetValueOrDefault("boss-defeated")} " +
                $"boss_saved_pre_completion={_bossDefeatPersistedBeforeCompletion}");
            string markdown = ProloguePlaytestReportFormatter.ToMarkdown(report);
            foreach (string field in new[]
                     { "Sectors visited", "First checkpoint", "Grapple failures by reason",
                         "Shield overheats", "Mode switches", "Selected slots" })
                TestAssert.True(markdown.Contains(field, StringComparison.Ordinal),
                    $"runtime Markdown exposes migrated field {field}");
            GD.Print($"TASK9_TELEMETRY_RUNTIME_OK source=AutomatedFixture production_hands_on_silent=True " +
                $"sectors={report.SectorsVisited.Count} checkpoints={report.CheckpointsActivated.Count} " +
                $"ice={iceFailures} overheats={report.ShieldOverheats} attempts={report.BossAttempts} " +
                $"completed={report.Completed} close_flush=True stale_shield_ignored=True " +
                $"shortcut_requests=1 boss_requests=1 boss_saved_pre_completion=True");
        }

        private async Task VerifyPhysicalDeath(Task9GeneratedMainContext context,
            PrologueSectorId expectedSector, bool expectedActivated, string marker)
        {
            PrologueCheckpointSnapshot checkpoint = context.Level.Progress.RestoreCheckpoint();
            TestAssert.True(checkpoint.SectorId == expectedSector &&
                checkpoint.HasActivatedCheckpoint == expectedActivated,
                $"{marker} uses only the expected explicit checkpoint authority");
            CharacterBody2D player = context.Player;
            Knight.Combat.HealthComponent health = player.GetNode<Knight.Combat.HealthComponent>("Health");
            Knight.Player.PlayerEnergyController energy = player.GetNode<Knight.Player.PlayerEnergyController>("Energy");
            Knight.Player.PlayerShieldController shield = player.GetNode<Knight.Player.PlayerShieldController>("Shield");
            Knight.Abilities.MechanicalClawController claw =
                player.GetNode<Knight.Abilities.MechanicalClawController>("MechanicalClaw");
            Knight.Player.PlayerMotor motor = player.GetNode<Knight.Player.PlayerMotor>("Motor");
            Knight.Player.PlayerCombat combat = player.GetNode<Knight.Player.PlayerCombat>("Combat");

            int requested = 0;
            int completed = 0;
            void Requested(string _) => requested++;
            void Completed(string _) => completed++;
            context.Level.RecoveryRequested += Requested;
            context.Level.RecoveryCompleted += Completed;
            try
            {
                bool inBossSector = marker == "boss-retry";
                _session.Advance(.1d);
                _session.RecordDeath(inBossSector);
                _session.BeginRecovery("DEATH RESET", inBossSector);
                TestAssert.True(health.ApplyDamage(new Knight.Combat.DamageRequest(
                        health.CurrentHealth, Vector2.Down, 0f,
                        910000UL + (ulong)expectedSector * 10UL + (ulong)requested)),
                    $"{marker} reaches a real player HealthComponent death");
                TestAssert.True(requested == 1 && completed == 1,
                    $"{marker} recovery begins and completes in the same physics frame");
                TestAssert.True(player.GlobalPosition.DistanceTo(checkpoint.WorldPosition) < 1.1f,
                    $"{marker} restores the exact checkpoint world position immediately");
                TestAssert.True(health.CurrentHealth == health.MaxHealth &&
                    energy.EnergyRatio >= .999f && energy.HeatRatio <= .001f &&
                    !shield.IsActive &&
                    claw.CurrentFrame.State == Knight.Abilities.MechanicalClawState.Idle &&
                    player.Velocity == Vector2.Zero && !combat.IsAttacking &&
                    motor.AcceptedDashCount == 0,
                    $"{marker} synchronously clears health/energy/heat/shield/claw/combat/motor/velocity transients");
                TestAssert.True(context.Level.Progress.BellChainShortcutOpen == checkpoint.ShortcutOpen &&
                    context.Level.Progress.Discoveries.SetEquals(checkpoint.Progress.Discoveries),
                    $"{marker} preserves shortcut and discoveries");
                _session.FinishRecovery();
                GD.Print($"TASK9_GENERATED_MAIN_DEATH_OK marker={marker} " +
                    $"sector={expectedSector} activated={expectedActivated} same_frame=True " +
                    $"position={player.GlobalPosition}");
                await context.AdvancePhysics(3);
            }
            finally
            {
                context.Level.RecoveryRequested -= Requested;
                context.Level.RecoveryCompleted -= Completed;
            }
        }

        private void OnProgressPersistRequested(string reason)
        {
            _persistRequests[reason] = _persistRequests.GetValueOrDefault(reason) + 1;
            if (reason != "boss-defeated") return;
            string savePath = _flow.SavePath.StartsWith("user://", StringComparison.Ordinal)
                ? ProjectSettings.GlobalizePath(_flow.SavePath)
                : Path.GetFullPath(_flow.SavePath);
            if (!File.Exists(savePath)) return;
            string json = File.ReadAllText(savePath);
            _bossDefeatPersistedBeforeCompletion =
                _completion.CurrentObjective == PrologueObjective.DefeatBellkeeper &&
                json.Contains("\"bossDefeated\": true", StringComparison.Ordinal) &&
                json.Contains("\"completed\": false", StringComparison.Ordinal);
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

        private static void MoveMouse(CharacterBody2D player, Vector2 worldPosition)
        {
            // Keep the display viewport and InputMap on the same normal MouseMotion event.
            // PlayerInputReader observes it through its production input adapter, so playback
            // does not rely on an OS cursor warp in the transformed five-sector canvas.
            Vector2 viewportPosition = player.GetViewport().GetFinalTransform() *
                (player.GetGlobalTransformWithCanvas() * player.ToLocal(worldPosition));
            var motion = new InputEventMouseMotion
            {
                Position = viewportPosition,
                GlobalPosition = viewportPosition
            };
            player.GetViewport().PushInput(motion, true);
            Input.ParseInputEvent(motion);
            Input.FlushBufferedEvents();
        }

        private static void SendMouse(CharacterBody2D player, MouseButton button, bool pressed,
            Vector2 worldPosition)
        {
            MoveMouse(player, worldPosition);
            Input.ParseInputEvent(new InputEventMouseButton
            {
                ButtonIndex = button,
                Pressed = pressed,
                Position = player.GetViewport().GetFinalTransform() *
                    (player.GetGlobalTransformWithCanvas() * player.ToLocal(worldPosition)),
                GlobalPosition = player.GetViewport().GetFinalTransform() *
                    (player.GetGlobalTransformWithCanvas() * player.ToLocal(worldPosition)),
                ButtonMask = pressed ? button == MouseButton.Left
                    ? MouseButtonMask.Left
                    : MouseButtonMask.Right
                    : (MouseButtonMask)0
            });
            Input.FlushBufferedEvents();
        }
    }
}
