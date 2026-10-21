using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;
using Knight.Combat;
using Knight.Presentation.Minimap;
using Knight.Prologue;
using Knight.Prologue.Collections;
using Knight.Prologue.Seamless;
using Knight.Save;
using Knight.Shared;
using Knight.Tests.Presentation;

namespace Knight.Tests.Integration;

public static partial class PrologueCanyonGeneratedMainRouteTests
{
    // Detects Continue accidentally starting over, dropping real collected IDs or
    // explored terrain, restoring the wrong floor, or leaving movement disabled.
    // Inputs are byte-for-byte copies of a terminated, real GL route's save, not
    // serialized test progress. One new process; same actor only within this process.
    internal static async Task RunColdContinue(SceneTree tree)
    {
        Task10ApplicationPersistenceGuard persistence = Task10ApplicationPersistenceGuard.CreateFromArguments();
        string inputRoot = Path.Combine(Directory.GetParent(persistence.CaseRoot)!.FullName, "continuation-input");
        const string saveSha = "D6BD5F9DA31396A43C0BDB4E4C50F3EA2CF99B1746E5D099CAD39935BDB5C85C";
        const string settingsSha = "D33D349E8DF79647B14E95F3AD61B895E3D12ED192C23E4DF31AB693416A2FB8";
        string[] fragments = ["canyon.v2.fragment.abyss-floor", "canyon.v2.fragment.entrance", "canyon.v2.fragment.rift"];
        PrologueLevelController level = null;
        KeyboardRouteInputDriver keyboard = null;
        RouteMonitor monitor = null;
        PrologueGameFlowController flow = null;
        PrologueGameFlowController.SessionStartedEventHandler started = null;
        string failure = null, observationFailure = null;
        bool completedProof = false;
        int sessions = 0, recoveryRequests = 0, recoveryCompletions = 0;
        ulong playerId = 0;
        Vector2 restoredPosition = default, finalPosition = default;
        ExplorationMapSaveData sessionMap = null;
        int restoredCells = 0, restoredBoundaries = 0;
        void Requested(string reason) => recoveryRequests++;
        void Recovered(string reason) => recoveryCompletions++;
        try
        {
            foreach (var (name, expected) in new[] { ("save.json", saveSha), ("settings.json", settingsSha) })
            {
                string source = Path.Combine(inputRoot, name);
                for (DirectoryInfo parent = new(inputRoot); parent != null; parent = parent.Parent)
                    TestAssert.True(parent.Exists && (parent.Attributes & FileAttributes.ReparsePoint) == 0,
                        "cold Continue input has no reparse ancestors");
                TestAssert.True((File.GetAttributes(source) & FileAttributes.ReparsePoint) == 0 &&
                    Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(source))) == expected,
                    "cold Continue input matches the actual retained GL gameplay save/settings");
                File.Copy(source, persistence.ResolveWritablePath(name), overwrite: false);
            }
            using JsonDocument sourceSave = JsonDocument.Parse(File.ReadAllText(Path.Combine(inputRoot, "save.json")));
            JsonElement sourceMap = sourceSave.RootElement.GetProperty("explorationMap");
            string cells = sourceMap.GetProperty("freeCellsBase64").GetString();
            string boundaries = sourceMap.GetProperty("boundarySegmentsBase64").GetString();
            string terrainRevision = sourceMap.GetProperty("terrainRevision").GetString();
            TestAssert.True(CountMapBits(cells) == 19898 && CountMapBits(boundaries) == 878,
                "source contains nonempty real GL exploration, not the empty headless map");

            using PackedScene packed = GD.Load<PackedScene>(MainPath);
            level = packed.Instantiate<PrologueLevelController>();
            flow = ConfigureTask10Persistence(level, persistence);
            flow.HeadlessAutoStart = false;
            flow.AutomationAutoStart = false;
            // Subscribe before mount/Continue; the later movement monitor alone
            // would miss a wrong recovery during session initialization.
            level.RecoveryRequested += Requested;
            level.RecoveryCompleted += Recovered;
            CharacterBody2D player = level.GetNode<CharacterBody2D>("PlayerRoot/Player");
            playerId = player.GetInstanceId();
            TestAssert.Equal(new Vector2(160, 600), player.Position, "cold process instantiates the authored spawn before Continue");
            keyboard = KeyboardRouteInputDriver.Begin(player);
            started = () =>
            {
                sessions++;
                try
                {
                    restoredPosition = player.GlobalPosition;
                    sessionMap = level.GetNode<PrologueMinimapController>("MinimapController").CaptureExploration();
                }
                catch (Exception error) { observationFailure = error.ToString(); }
            };
            flow.SessionStarted += started;
            tree.Root.AddChild(level);
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            TestAssert.True(flow.State == PrologueGameFlowState.Title && flow.HasSave &&
                flow.LoadResult.Status == PrologueSaveLoadStatus.Current && sessions == 0,
                "new process recognizes the copied current slot without auto-start or migration");
            Button start = level.GetNode<Button>("ProductUi/MainMenu/Panel/Buttons/Start");
            Button resume = level.GetNode<Button>("ProductUi/MainMenu/Panel/Buttons/Continue");
            TestAssert.True(start.HasFocus() && !resume.Disabled, "actual title starts focused on Start with Continue available");
            SendColdMenuKey(level, Key.Down, true);
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            SendColdMenuKey(level, Key.Down, false);
            TestAssert.True(resume.HasFocus(), "one actual Down event selects Continue without programmatic focus or flow calls");
            SendColdMenuKey(level, Key.Enter, true);
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            SendColdMenuKey(level, Key.Enter, false);
            TestAssert.True(sessions == 1 && recoveryRequests == 0 && recoveryCompletions == 0 && observationFailure == null &&
                flow.State == PrologueGameFlowState.Playing && restoredPosition == new Vector2(1680, 3960),
                "Continue starts exactly one session at the real saved camp, not spawn or next node");
            TestAssert.True(sessionMap != null && sessionMap.FormatVersion == 1 &&
                sessionMap.TerrainRevision == terrainRevision && sessionMap.FreeCellsBase64 == cells &&
                sessionMap.BoundarySegmentsBase64 == boundaries,
                "at session start, every saved explored and unexplored bit is exactly restored");
            restoredCells = CountMapBits(sessionMap.FreeCellsBase64);
            restoredBoundaries = CountMapBits(sessionMap.BoundarySegmentsBase64);

            SeamlessPrologueWorld world = level.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors");
            // Cold Continue starts mid-route; use the existing current-sector
            // adjacency mode, not the default forward-from-entrance assumption.
            monitor = new RouteMonitor(level, player, player.GetNode<Camera2D>("Camera2D"), world, playerId,
                allowRouteBacktracking: true);
            await PhysicsFrames(tree, monitor, 24);
            TestAssert.True(world.TryGetMountedSector(PrologueSectorId.CanyonBasin, out var basin), "Continue mounts the saved basin");
            StaticBody2D floor = basin.GetNode<StaticBody2D>("Gameplay/Collision/BasinCampFloor");
            HealthComponent health = player.GetNode<HealthComponent>("Health");
            TestAssert.True(player == level.Player && player.GetInstanceId() == playerId &&
                level.GetNode("PlayerRoot").GetChildCount() == 1 && player.IsVisibleInTree() &&
                player.IsPhysicsProcessing() && !tree.Paused && health.CurrentHealth == health.MaxHealth &&
                player.GlobalPosition.DistanceTo(new Vector2(1680, 3960)) < 2 && HasFloorContact(player, floor),
                "the cold-loaded actor is visible, healthy and physically supported on its checkpoint");
            monitor.AssertCameraCoverage();
            monitor.AssertFullCapsuleVisible();
            TestAssert.True(fragments.SequenceEqual(flow.Save.CollectedFragmentIds.OrderBy(id => id, StringComparer.Ordinal)) &&
                fragments.SequenceEqual(level.Progress.CollectedFragmentIds.OrderBy(id => id, StringComparer.Ordinal)) &&
                level.Progress.RestoreCheckpoint().CheckpointId == "canyon.v2.basin-camp" &&
                !level.Progress.LegacyCollectionQuotaExempt && !BossAdmissionModel.Evaluate(level.Progress).Allowed &&
                BossAdmissionModel.Evaluate(level.Progress).MissingFragments == 3,
                "real three IDs survive reload without duplicate, loss or accidental six-fragment exemption");
            float initialX = player.GlobalPosition.X;
            keyboard.Command(InputActions.MoveRight, true);
            await PhysicsFrames(tree, monitor, 10);
            keyboard.ReleaseAll();
            await PhysicsFrames(tree, monitor, 12);
            finalPosition = player.GlobalPosition;
            TestAssert.True(finalPosition.X > initialX + 20 && HasFloorContact(player, floor) &&
                health.CurrentHealth == health.MaxHealth && flow.State == PrologueGameFlowState.Playing && sessions == 1 &&
                recoveryRequests == 0 && recoveryCompletions == 0,
                "fresh D moves the cold-loaded actor safely without a new session or recovery");
            monitor.AssertTask9Recoveries("cold Continue", 0);
            TestAssert.Equal(saveSha, Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(flow.SavePath))),
                "Continue and safe movement do not replace or rewrite the original progress slot");
            completedProof = true;
            GD.Print($"COLD_CONTINUE_OK menu=Down,Enter sessions={sessions} checkpoint=basin-camp fragments=3 missing=3 " +
                $"explored_cells={restoredCells} explored_boundaries={restoredBoundaries} player={playerId} " +
                $"restored={restoredPosition} moved={finalPosition} supported=true source=real_GL_save OS_origin=false full_route=false");
        }
        catch (Exception error) { failure = error.ToString(); throw; }
        finally
        {
            try
            {
                File.WriteAllText(persistence.ResolveWritablePath("cold-continue-result.json"), JsonSerializer.Serialize(new
                {
                    completedProof, failure, observationFailure, sessions, recoveryRequests, recoveryCompletions, playerId, sourceSaveSha256 = saveSha,
                    sourceSettingsSha256 = settingsSha, restoredCells, restoredBoundaries,
                    restoredX = restoredPosition.X, restoredY = restoredPosition.Y,
                    finalX = finalPosition.X, finalY = finalPosition.Y,
                    inputBasis = "engine Down/Enter/D key events, not OS/human", fullRoute = false
                }, new JsonSerializerOptions { WriteIndented = true }));
            }
            finally
            {
                try
                {
                    if (GodotObject.IsInstanceValid(level) && level.IsInsideTree())
                    {
                        SendColdMenuKey(level, Key.Down, false);
                        SendColdMenuKey(level, Key.Enter, false);
                    }
                    keyboard?.ReleaseAll();
                    monitor?.Dispose();
                    keyboard?.Dispose();
                    if (GodotObject.IsInstanceValid(flow) && started != null) flow.SessionStarted -= started;
                    if (GodotObject.IsInstanceValid(level))
                    {
                        level.RecoveryRequested -= Requested;
                        level.RecoveryCompleted -= Recovered;
                    }
                }
                finally
                {
                    try
                    {
                        if (GodotObject.IsInstanceValid(level))
                        {
                            if (level.IsInsideTree()) await TestSceneTeardown.FreeSceneAfterAudioDrain(tree, level);
                            else level.Free();
                        }
                    }
                    finally { persistence.AssertProtectedDefaultsUnchanged(); }
                }
            }
        }
    }

    private static int CountMapBits(string encoded) => Convert.FromBase64String(encoded)
        .Sum(value => System.Numerics.BitOperations.PopCount((uint)value));

    private static void SendColdMenuKey(Node canvas, Key key, bool pressed)
    {
        using var input = new InputEventKey { PhysicalKeycode = key, Keycode = key, Pressed = pressed };
        bool isDown = input.IsAction("ui_down");
        GD.Print($"COLD_MENU_KEY phase=before key={key} pressed={pressed} down={isDown} focus={canvas.GetViewport().GuiGetFocusOwner()?.GetPath()}");
        // Parse already forwards to the viewport. PushInput as well would make a
        // single Down navigate twice (Start -> Continue -> Settings).
        Input.ParseInputEvent(input);
        Input.FlushBufferedEvents();
        GD.Print($"COLD_MENU_KEY phase=after_parse key={key} pressed={pressed} focus={canvas.GetViewport().GuiGetFocusOwner()?.GetPath()}");
    }
}
