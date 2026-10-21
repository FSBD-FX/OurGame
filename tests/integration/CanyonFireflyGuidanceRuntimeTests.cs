using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Knight.Abilities;
using Knight.Boss;
using Knight.Combat;
using Knight.Enemies;
using Knight.Enemies.Laborer;
using Knight.Enemies.Moth;
using Knight.Mechanisms;
using Knight.Prologue;
using Knight.Prologue.Guidance;
using Knight.Prologue.Seamless;
using Knight.Prologue.Sample;

namespace Knight.Tests.Integration;

public static class CanyonFireflyGuidanceRuntimeTests
{
    public static async Task Run(SceneTree tree)
    {
        await ActualAttackPublishersDriveTheAdapter(tree);
        await GeneratedRouteEligibilityAndLayoutGate(tree);
    }

    private static async Task ActualAttackPublishersDriveTheAdapter(SceneTree tree)
    {
        await ExerciseRust(tree);
        await ExerciseLaborer(tree);
        await ExerciseMoth(tree);
        await ExerciseBoss(tree);
    }

    private static async Task ExerciseRust(SceneTree tree)
    {
        var fixture = await Create<RustShellSquire>(tree,
            "res://scenes/enemies/RustShellSquire.tscn", new Vector2(45f, 0f));
        try
        {
            fixture.Actor.Gravity = 0f;
            fixture.Actor._PhysicsProcess(.01);
            TestAssert.Equal(AttackPhase.Telegraph, fixture.Actor.CurrentFrame.AttackTimeline.Phase,
                "actual Rust-Shell publisher enters Telegraph");
            TestAssert.True(CanyonCombatTelegraphAdapter.IsTelegraphActive(fixture.Actor),
                "adapter reads Rust-Shell CurrentFrame.AttackTimeline");
            fixture.Actor._PhysicsProcess(.70);
            TestAssert.True(!CanyonCombatTelegraphAdapter.IsTelegraphActive(fixture.Actor),
                "Rust-Shell Active does not leave a stale guidance deferral");
            fixture.Actor._PhysicsProcess(.20);
            TestAssert.Equal(AttackPhase.Recovery, fixture.Actor.CurrentFrame.AttackTimeline.Phase,
                "actual Rust-Shell publisher reaches Recovery");
            TestAssert.True(!CanyonCombatTelegraphAdapter.IsTelegraphActive(fixture.Actor),
                "Rust-Shell Recovery is not a telegraph");
            fixture.Root.RemoveChild(fixture.Actor);
            try
            {
                TestAssert.True(!CanyonCombatTelegraphAdapter.IsTelegraphActive(fixture.Actor),
                    "adapter rejects an actual publisher after it is unmounted");
            }
            finally { fixture.Actor.Free(); }
        }
        finally { fixture.Root.Free(); }
    }

    private static async Task ExerciseLaborer(SceneTree tree)
    {
        var fixture = await Create<BellBackLaborer>(tree,
            "res://scenes/enemies/BellBackLaborer.tscn", new Vector2(70f, 0f));
        try
        {
            fixture.Actor.Tuning.Gravity = 0f;
            fixture.Actor._PhysicsProcess(.01);
            fixture.Actor._PhysicsProcess(.45);
            TestAssert.Equal(AttackPhase.Telegraph, fixture.Actor.CurrentFrame.AttackTimeline.Phase,
                "actual Bell-Back publisher enters Telegraph");
            TestAssert.True(CanyonCombatTelegraphAdapter.IsTelegraphActive(fixture.Actor),
                "adapter reads Bell-Back CurrentFrame.AttackTimeline");
            fixture.Actor._PhysicsProcess(.75);
            TestAssert.True(!CanyonCombatTelegraphAdapter.IsTelegraphActive(fixture.Actor),
                "Bell-Back Active clears guidance deferral");
            fixture.Actor._PhysicsProcess(.25);
            TestAssert.True(!CanyonCombatTelegraphAdapter.IsTelegraphActive(fixture.Actor),
                "Bell-Back Recovery clears guidance deferral");
        }
        finally { fixture.Root.Free(); }
    }

    private static async Task ExerciseMoth(SceneTree tree)
    {
        var fixture = await Create<GraveLampMoth>(tree,
            "res://scenes/enemies/GraveLampMoth.tscn", new Vector2(0f, 140f));
        try
        {
            fixture.Actor._PhysicsProcess(.01);
            fixture.Actor._PhysicsProcess(.01);
            fixture.Actor._PhysicsProcess(.45);
            TestAssert.Equal(AttackPhase.Telegraph, fixture.Actor.CurrentFrame.AttackTimeline.Phase,
                "actual Grave-Lamp publisher enters Telegraph");
            TestAssert.True(CanyonCombatTelegraphAdapter.IsTelegraphActive(fixture.Actor),
                "adapter reads Grave-Lamp CurrentFrame.AttackTimeline");
            fixture.Actor._PhysicsProcess(.25);
            TestAssert.True(!CanyonCombatTelegraphAdapter.IsTelegraphActive(fixture.Actor),
                "Grave-Lamp Active clears guidance deferral");
            fixture.Actor._PhysicsProcess(.36);
            TestAssert.True(!CanyonCombatTelegraphAdapter.IsTelegraphActive(fixture.Actor),
                "Grave-Lamp Recovery clears guidance deferral");
        }
        finally { fixture.Root.Free(); }
    }

    private static async Task ExerciseBoss(SceneTree tree)
    {
        var fixture = await Create<FallenBellkeeper>(tree,
            "res://scenes/boss/FallenBellkeeper.tscn", new Vector2(-150f, 20f));
        try
        {
            fixture.Actor.Gravity = 0f;
            fixture.Actor.ActivateEncounter();
            fixture.Actor._PhysicsProcess(.01);
            fixture.Actor._PhysicsProcess(1.05);
            var visual = fixture.Actor.GetNode<Knight.Presentation.AttackTelegraphVisual>(
                "VisualRoot/AttackTelegraph");
            TestAssert.Equal(FallenBellkeeperState.PhaseOne, fixture.Actor.CurrentFrame.State,
                "actual Boss coarse state remains PhaseOne during sweep windup");
            TestAssert.Equal(AttackPhase.Telegraph, fixture.Actor.CurrentFrame.AttackTimeline.Phase,
                "actual Boss sweep publisher enters Telegraph");
            TestAssert.True(!visual.DangerVisible,
                "actual pre-lock Boss Telegraph has DangerVisible false");
            TestAssert.True(CanyonCombatTelegraphAdapter.IsTelegraphActive(fixture.Actor),
                "adapter defers on actual pre-lock PhaseOne sweep independent of DangerVisible");
            fixture.Actor._PhysicsProcess(.85);
            TestAssert.True(!CanyonCombatTelegraphAdapter.IsTelegraphActive(fixture.Actor),
                "Boss non-Telegraph phase clears guidance deferral");
        }
        finally { fixture.Root.Free(); }
    }

    private static async Task<Fixture<T>> Create<T>(SceneTree tree, string path,
        Vector2 playerPosition)
        where T : Node2D
    {
        var root = new Node2D { Name = typeof(T).Name + "FireflyPublisherFixture" };
        var player = new Node2D { Name = "Player", Position = playerPosition };
        player.AddToGroup("player");
        T actor = GD.Load<PackedScene>(path).Instantiate<T>();
        root.AddChild(player);
        root.AddChild(actor);
        tree.Root.AddChild(root);
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        return new Fixture<T>(root, actor);
    }

    private sealed record Fixture<T>(Node2D Root, T Actor) where T : Node2D;

    private static async Task GeneratedRouteEligibilityAndLayoutGate(SceneTree tree)
    {
        string stateRoot = Path.Combine(Path.GetTempPath(),
            "knight-task8-firefly-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(stateRoot);
        PrologueLevelController level = GD.Load<PackedScene>(
            "res://scenes/prologue/PrologueLevel.tscn").Instantiate<PrologueLevelController>();
        PrologueGameFlowController flow = level.GetNode<PrologueGameFlowController>("GameFlow");
        flow.AutomationAutoStart = true;
        flow.SavePath = Path.Combine(stateRoot, "save.json");
        flow.SettingsPath = Path.Combine(stateRoot, "settings.json");
        tree.Root.AddChild(level);
        try
        {
            for (int i = 0; i < 5; i++)
            {
                await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
                await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            }
            var world = level.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors");
            var controller = level.GetNodeOrNull<CanyonFireflyGuidanceController>(
                "WorldRoot/FireflyGuidance");
            TestAssert.True(controller != null,
                "generated main mounts one world-level firefly controller outside streamed sectors");
            TestAssert.Equal(1, level.GetTree().GetNodesInGroup("canyon_firefly_guidance").Count,
                "generated main has exactly one permanent firefly controller");
            TestAssert.True(controller.IsLayoutActive &&
                            world.LayoutVersion == CanyonWorldLayout.CurrentLayoutVersion,
                "layout2 generated main enables route-graph guidance");

            VerifyActualPauseFreezesEveryTimer(tree, level, flow, controller);
            VerifyNearestMarkerUsesLiveCandidates(tree, level, world, controller);
            await VerifyNearestMarkerUsesLiveGates(tree, level, world, controller);

            Rect2 all = CanyonWorldLayout.DesignBounds;
            TestAssert.True(CanyonFireflyGuidanceController.IsMarkerEligible(world, level.Progress,
                    CanyonRouteNodeId.LowCeilingBridge, CanyonRouteNodeId.StaggeredRift, all, out _),
                "mounted ordinary forward route marker is eligible");

            TestAssert.True(CanyonFireflyGuidanceController.IsMarkerEligible(world, level.Progress,
                    CanyonRouteNodeId.LowCeilingBridge, CanyonRouteNodeId.StaggeredRift, all,
                    out Marker2D materialMarker),
                "eligible route resolves its actual mounted marker");
            TestAssert.True(CanyonFireflyGuidanceController.TryResolveSupport(materialMarker,
                    CanyonRouteNodeId.StaggeredRift, out GrappleSurface2D support),
                "eligible marker resolves its actual mounted support surface");
            GrappleSurfaceMaterial original = support.Material;
            try
            {
                support.Material = GrappleSurfaceMaterial.Ice;
                TestAssert.True(!CanyonFireflyGuidanceController.IsMarkerEligible(world, level.Progress,
                        CanyonRouteNodeId.LowCeilingBridge, CanyonRouteNodeId.StaggeredRift, all, out _),
                    "Ice support independently rejects the otherwise reachable marker");
                support.Material = GrappleSurfaceMaterial.Forbidden;
                TestAssert.True(!CanyonFireflyGuidanceController.IsMarkerEligible(world, level.Progress,
                        CanyonRouteNodeId.LowCeilingBridge, CanyonRouteNodeId.StaggeredRift, all, out _),
                    "Forbidden support independently rejects the otherwise reachable marker");
            }
            finally { support.Material = original; }
            TestAssert.True(CanyonFireflyGuidanceController.IsMarkerEligible(world, level.Progress,
                    CanyonRouteNodeId.LowCeilingBridge, CanyonRouteNodeId.StaggeredRift, all, out _),
                "restoring the exact support material restores eligibility");
            TestAssert.True(!CanyonFireflyGuidanceController.IsMarkerEligible(world, level.Progress,
                    CanyonRouteNodeId.LowCeilingBridge, CanyonRouteNodeId.StaggeredRift,
                    new Rect2(0, 0, 100, 100), out _),
                "offscreen required target is rejected even when graph-reachable");

            TestAssert.True(world.RestoreCheckpoint(PrologueSectorId.CanyonBasin,
                    CanyonWorldLayout.CheckpointWorldPositions["canyon.v2.basin-camp"]),
                "fixture can mount the Basin neighborhood through the actual checkpoint transition");
            for (int i = 0; i < 2; i++)
            {
                await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
                await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            }
            VerifyRemoteSameSectorMechanismDoesNotDefer(level, controller);
            await VerifyOutOfEncounterPublisherDoesNotDefer(tree, level, controller);
            var shortcutMutation = new PrologueProgress();
            TestAssert.True(!CanyonFireflyGuidanceController.IsMarkerEligible(world, shortcutMutation,
                    CanyonRouteNodeId.SplitLevelEncounter, CanyonRouteNodeId.BasinCamp, all, out _),
                "closed upper-basin shortcut independently blocks its reverse marker");
            shortcutMutation.OpenShortcut(CanyonWorldLayout.UpperBasinShortcutId);
            TestAssert.True(CanyonFireflyGuidanceController.IsMarkerEligible(world, shortcutMutation,
                    CanyonRouteNodeId.SplitLevelEncounter, CanyonRouteNodeId.BasinCamp, all, out _),
                "opening the exact shortcut admits its mounted Basin marker");
            shortcutMutation.Reset();
            TestAssert.True(!CanyonFireflyGuidanceController.IsMarkerEligible(world, shortcutMutation,
                    CanyonRouteNodeId.SplitLevelEncounter, CanyonRouteNodeId.BasinCamp, all, out _),
                "removing the exact shortcut independently restores the closed gate");
            shortcutMutation.OpenShortcut(CanyonWorldLayout.UpperBasinShortcutId);
            TestAssert.True(CanyonFireflyGuidanceController.IsMarkerEligible(world, shortcutMutation,
                    CanyonRouteNodeId.SplitLevelEncounter, CanyonRouteNodeId.BasinCamp, all, out _),
                "restoring the exact shortcut source restores marker eligibility");
            TestAssert.True(!CanyonFireflyGuidanceController.IsMarkerEligible(world, shortcutMutation,
                    CanyonRouteNodeId.SealedCourtyard, CanyonRouteNodeId.BossHall, all, out _),
                "open shortcut cannot borrow authority from the closed completed-seal gate");

            TestAssert.True(world.RestoreCheckpoint(PrologueSectorId.LostTemple,
                    CanyonWorldLayout.CheckpointWorldPositions["canyon.v2.temple-forecourt"]),
                "fixture can mount the Temple neighborhood through the actual checkpoint transition");
            for (int i = 0; i < 2; i++)
            {
                await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
                await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            }
            var sealMutation = new PrologueProgress();
            TestAssert.True(!CanyonFireflyGuidanceController.IsMarkerEligible(world, sealMutation,
                    CanyonRouteNodeId.SealedCourtyard, CanyonRouteNodeId.BossHall, all, out _),
                "missing completed-seal state independently blocks BossHall");
            sealMutation.ActivateMechanism(CanyonWorldLayout.CourtyardSealMechanismId);
            TestAssert.True(CanyonFireflyGuidanceController.IsMarkerEligible(world, sealMutation,
                    CanyonRouteNodeId.SealedCourtyard, CanyonRouteNodeId.BossHall, all, out _),
                "the exact completed-seal activation admits BossHall");
            sealMutation.Reset();
            TestAssert.True(!CanyonFireflyGuidanceController.IsMarkerEligible(world, sealMutation,
                    CanyonRouteNodeId.SealedCourtyard, CanyonRouteNodeId.BossHall, all, out _),
                "removing the completed-seal source restores the closed BossHall gate");
            sealMutation.ActivateMechanism(CanyonWorldLayout.CourtyardSealMechanismId);
            TestAssert.True(CanyonFireflyGuidanceController.IsMarkerEligible(world, sealMutation,
                    CanyonRouteNodeId.SealedCourtyard, CanyonRouteNodeId.BossHall, all, out _),
                "restoring the exact completed-seal source restores BossHall eligibility");
            level.Progress.ActivateMechanism(CanyonWorldLayout.CourtyardSealMechanismId);
            TestAssert.True(!CanyonFireflyGuidanceController.IsMarkerEligible(world, level.Progress,
                    CanyonRouteNodeId.BossHall, CanyonRouteNodeId.SafeExit, all, out _),
                "Boss exit stays ineligible before the exact Boss result");
            level.Progress.MarkBossDefeated();
            TestAssert.True(CanyonRouteGraph.Reachable(CanyonRouteNodeId.BossHall,
                    level.Progress.OpenShortcuts, level.Progress.ActivatedMechanisms,
                    level.Progress.BossDefeated).Contains(CanyonRouteNodeId.SafeExit),
                "the exact Boss result opens the SafeExit graph edge");
            TestAssert.True(world.TryGetMountedSector(PrologueSectorId.LostTemple,
                    out PrologueSector temple),
                "Boss-exit resolution retains the mounted Temple sector");
            Marker2D safeExit = temple.GetNodeOrNull<Marker2D>("Gameplay/MicroScenes/SafeExit");
            TestAssert.True(safeExit != null && safeExit.IsInsideTree() && safeExit.IsVisibleInTree(),
                "the actual authored SafeExit marker remains mounted and visible");
            TestAssert.True(all.HasPoint(safeExit.GlobalPosition),
                $"SafeExit marker stays in design bounds, actual={safeExit.GlobalPosition}");
            TestAssert.True(CanyonFireflyGuidanceController.TryResolveSupport(safeExit,
                    CanyonRouteNodeId.SafeExit, out GrappleSurface2D exitDoor) &&
                exitDoor.Material == GrappleSurfaceMaterial.Forbidden,
                "the non-grapple SafeExit rest marker is not misclassified by its nearby closed-door profile");
            TestAssert.True(CanyonFireflyGuidanceController.IsMarkerEligible(world, level.Progress,
                    CanyonRouteNodeId.BossHall, CanyonRouteNodeId.SafeExit, all, out _),
                "Boss result admits the authored SafeExit marker");
            TestAssert.True(!CanyonFireflyGuidanceController.IsMarkerEligible(world, level.Progress,
                    CanyonRouteNodeId.LowCeilingBridge, CanyonRouteNodeId.StaggeredRift, all, out _),
                "a graph-reachable marker remains rejected after its streamed sector unloads");
            await VerifyLifecycleCleanupAndReentry(tree, level, flow, world, controller, safeExit);
            await VerifyLayout1SampleWhileParentResourcesAreLive(tree);
        }
        finally
        {
            await TestSceneTeardown.FreeSceneAfterAudioDrain(tree, level);
            if (Directory.Exists(stateRoot)) Directory.Delete(stateRoot, true);
        }
    }

    // Break caught: nearest selection must not bypass live eligibility or cache
    // a marker reference/order when reusing a reachable list within one call.
    private static void VerifyNearestMarkerUsesLiveCandidates(SceneTree tree,
        PrologueLevelController level, SeamlessPrologueWorld world,
        CanyonFireflyGuidanceController controller)
    {
        TestAssert.True(world.TryGetMountedSector(PrologueSectorId.CanyonDescent,
            out PrologueSector descent), "nearest fixture retains the actual mounted Descent");
        var rift = descent.GetNode<Marker2D>("Gameplay/MicroScenes/StaggeredRift");
        var archive = descent.GetNode<Marker2D>("Gameplay/MicroScenes/ArchiveSideCave");
        Node parent = rift.GetParent();
        Vector2 oldRift = rift.GlobalPosition, oldArchive = archive.GlobalPosition;
        Vector2 oldPlayer = level.Player.GlobalPosition;
        Vector2I oldSize = tree.Root.Size, oldScale = tree.Root.ContentScaleSize;
        var materials = new Dictionary<GrappleSurface2D, GrappleSurfaceMaterial>();
        Camera2D camera = level.Player.GetNode<Camera2D>("Camera2D");
        Marker2D replacement = null;
        try
        {
            tree.Root.Size = tree.Root.ContentScaleSize = new Vector2I(1920, 1080);
            level.Player.GlobalPosition = new Vector2(1279f, 600f);
            camera.ResetSmoothing(); camera.ForceUpdateScroll();
            // Hand-derived tie: both targets are521px right of the actor. The
            // authored graph visits StaggeredRift before ArchiveSideCave.
            rift.GlobalPosition = archive.GlobalPosition = new Vector2(1800f, 600f);
            foreach ((Marker2D marker, CanyonRouteNodeId id) in new[] {
                (rift, CanyonRouteNodeId.StaggeredRift), (archive, CanyonRouteNodeId.ArchiveSideCave) })
            {
                TestAssert.True(CanyonFireflyGuidanceController.TryResolveSupport(marker, id, out GrappleSurface2D surface),
                    "live nearest fixture resolves its real route support");
                materials.TryAdd(surface, surface.Material); surface.Material = GrappleSurfaceMaterial.TempleMetal;
            }
            controller.RefreshNow();
            TestAssert.True(controller.CurrentMarker == rift, "equal nearest distance retains authored graph order");
            rift.GlobalPosition = new Vector2(2000f, 600f); controller.RefreshNow();
            TestAssert.True(controller.CurrentMarker == archive, "next refresh uses live target distance");
            rift.GlobalPosition = new Vector2(1800f, 600f); rift.Hide(); controller.RefreshNow();
            TestAssert.True(controller.CurrentMarker == archive, "hidden nearest target is rejected without clearing state");
            rift.Show(); controller.RefreshNow();
            TestAssert.True(controller.CurrentMarker == rift, "shown nearest target immediately returns");
            parent.RemoveChild(rift); controller.RefreshNow();
            TestAssert.True(controller.CurrentMarker == archive, "removed nearest marker is not retained");
            replacement = new Marker2D { Name = "StaggeredRift", Position = new Vector2(520f, 600f) };
            parent.AddChild(replacement); replacement.GlobalPosition = new Vector2(1800f, 600f);
            controller.RefreshNow();
            TestAssert.True(controller.CurrentMarker == replacement, "same-path replacement resolves the new native marker");
            parent.RemoveChild(replacement); replacement.Free(); replacement = null;
            parent.AddChild(rift); controller.RefreshNow();
            TestAssert.True(controller.CurrentMarker == rift, "reentered original marker resolves immediately");
            TestAssert.True(CanyonFireflyGuidanceController.TryResolveSupport(rift,
                CanyonRouteNodeId.StaggeredRift, out GrappleSurface2D support), "live rift support retained");
            support.Material = GrappleSurfaceMaterial.Ice; controller.RefreshNow();
            TestAssert.True(controller.CurrentMarker == archive, "nearest path rejects live Ice support");
            support.Material = GrappleSurfaceMaterial.Forbidden; controller.RefreshNow();
            TestAssert.True(controller.CurrentMarker == archive, "nearest path rejects live Forbidden support");
            support.Material = GrappleSurfaceMaterial.TempleMetal; controller.RefreshNow();
            TestAssert.True(controller.CurrentMarker == rift, "restored support restores nearest eligibility");
            GD.Print("FIREFLY_NEAREST_LIVE_CANDIDATES_OK assertions=10");
        }
        finally
        {
            if (GodotObject.IsInstanceValid(replacement)) replacement.Free();
            if (rift.GetParent() == null) parent.AddChild(rift);
            rift.Show(); rift.GlobalPosition = oldRift; archive.GlobalPosition = oldArchive;
            foreach (var entry in materials) entry.Key.Material = entry.Value;
            level.Player.GlobalPosition = oldPlayer;
            tree.Root.Size = oldSize; tree.Root.ContentScaleSize = oldScale;
            camera.ResetSmoothing(); camera.ForceUpdateScroll(); controller.ClearTransient();
        }
    }

    private static async Task VerifyNearestMarkerUsesLiveGates(SceneTree tree,
        PrologueLevelController level, SeamlessPrologueWorld world,
        CanyonFireflyGuidanceController controller)
    {
        Vector2 oldPlayer = level.Player.GlobalPosition;
        Vector2I oldSize = tree.Root.Size, oldScale = tree.Root.ContentScaleSize;
        Camera2D camera = level.Player.GetNode<Camera2D>("Camera2D");
        var hidden = new List<Marker2D>();
        Marker2D basin = null; Vector2 oldBasin = default;
        try
        {
            tree.Root.Size = tree.Root.ContentScaleSize = new Vector2I(1920, 1080);
            TestAssert.True(world.RestoreCheckpoint(PrologueSectorId.CanyonBasin,
                CanyonWorldLayout.CheckpointWorldPositions["canyon.v2.basin-camp"]), "nearest gate fixture mounts Basin neighborhood");
            for (int i = 0; i < 2; i++) await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            TestAssert.True(world.TryGetMountedSector(PrologueSectorId.CanyonBasin, out PrologueSector basinSector),
                "nearest shortcut fixture retains Basin sector");
            basin = basinSector.GetNode<Marker2D>("Gameplay/MicroScenes/BasinCamp"); oldBasin = basin.GlobalPosition;
            foreach (PrologueSectorId id in world.MountedSectorIds)
            {
                world.TryGetMountedSector(id, out PrologueSector sector);
                foreach (Node child in sector.GetNode<Node>("Gameplay/MicroScenes").GetChildren())
                    if (child is Marker2D marker && marker != basin && marker.Visible)
                    { hidden.Add(marker); marker.Hide(); }
            }
            basin.GlobalPosition = new Vector2(4600f, 1600f);
            level.Player.GlobalPosition = new Vector2(4480f, 1600f); level.Player.Velocity = Vector2.Zero;
            camera.ResetSmoothing(); camera.ForceUpdateScroll();
            controller.RefreshNow();
            TestAssert.True(controller.CurrentMarker == null, "closed reverse shortcut rejects the only on-screen candidate");
            level.Progress.OpenShortcut(CanyonWorldLayout.UpperBasinShortcutId); controller.RefreshNow();
            TestAssert.True(controller.CurrentMarker == basin, "nearest path observes newly opened exact shortcut");
            level.Progress.Reset(); controller.RefreshNow();
            TestAssert.True(controller.CurrentMarker == null, "nearest path observes shortcut reset without stale reachability");
            level.Progress.OpenShortcut(CanyonWorldLayout.UpperBasinShortcutId); controller.RefreshNow();
            TestAssert.True(controller.CurrentMarker == basin, "nearest path observes restored shortcut");
            basin.GlobalPosition = oldBasin; foreach (Marker2D marker in hidden) marker.Show(); hidden.Clear();
            level.Progress.Reset();
            ulong oldBasinId = basin.GetInstanceId();
            TestAssert.True(world.RestoreCheckpoint(PrologueSectorId.LostTemple,
                CanyonWorldLayout.CheckpointWorldPositions["canyon.v2.temple-forecourt"]), "nearest gate fixture mounts Temple neighborhood");
            for (int i = 0; i < 2; i++) await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            TestAssert.True(world.TryGetMountedSector(PrologueSectorId.LostTemple, out PrologueSector temple), "nearest fixture retains Temple");
            Marker2D boss = temple.GetNode<Marker2D>("Gameplay/MicroScenes/BossHall");
            Marker2D exit = temple.GetNode<Marker2D>("Gameplay/MicroScenes/SafeExit");
            level.Player.GlobalPosition = new Vector2(5920f, 1080f); level.Player.Velocity = Vector2.Zero;
            camera.ResetSmoothing(); camera.ForceUpdateScroll(); controller.RefreshNow();
            TestAssert.True(controller.CurrentMarker == null, "closed seal and unloaded Basin yield no nearest target");
            level.Progress.ActivateMechanism(CanyonWorldLayout.CourtyardSealMechanismId); controller.RefreshNow();
            TestAssert.True(controller.CurrentMarker == boss, "nearest path observes exact completed seal");
            level.Progress.Reset(); controller.RefreshNow();
            TestAssert.True(controller.CurrentMarker == null, "nearest path observes seal reset");
            level.Progress.ActivateMechanism(CanyonWorldLayout.CourtyardSealMechanismId); controller.RefreshNow();
            TestAssert.True(controller.CurrentMarker == boss, "nearest path observes restored seal");
            level.Player.GlobalPosition = new Vector2(7040f, 1080f);
            camera.ResetSmoothing(); camera.ForceUpdateScroll(); controller.RefreshNow();
            TestAssert.True(controller.CurrentMarker == null, "self BossHall marker cannot masquerade as closed Boss exit");
            level.Progress.MarkBossDefeated(); controller.RefreshNow();
            TestAssert.True(controller.CurrentMarker == exit, "nearest path observes exact Boss result");
            level.Progress.Reset(); controller.RefreshNow();
            TestAssert.True(controller.CurrentMarker == null, "nearest path observes reset Boss result");
            TestAssert.True(world.RestoreCheckpoint(PrologueSectorId.CanyonBasin,
                CanyonWorldLayout.CheckpointWorldPositions["canyon.v2.basin-camp"]), "nearest fixture remounts Basin");
            for (int i = 0; i < 2; i++) await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            world.TryGetMountedSector(PrologueSectorId.CanyonBasin, out basinSector);
            basin = basinSector.GetNode<Marker2D>("Gameplay/MicroScenes/BasinCamp");
            TestAssert.True(basin.GetInstanceId() != oldBasinId, "streamed remount creates a different native marker");
            oldBasin = basin.GlobalPosition; basin.GlobalPosition = new Vector2(4600f, 1600f);
            level.Player.GlobalPosition = new Vector2(4480f, 1600f);
            camera.ResetSmoothing(); camera.ForceUpdateScroll();
            level.Progress.OpenShortcut(CanyonWorldLayout.UpperBasinShortcutId); controller.RefreshNow();
            TestAssert.True(controller.CurrentMarker == basin, "nearest path resolves remounted live target rather than cached native node");
            GD.Print("FIREFLY_NEAREST_LIVE_GATES_OK assertions=13");
        }
        finally
        {
            if (GodotObject.IsInstanceValid(basin)) basin.GlobalPosition = oldBasin;
            foreach (Marker2D marker in hidden) if (GodotObject.IsInstanceValid(marker)) marker.Show();
            level.Progress.Reset();
            world.RestoreCheckpoint(PrologueSectorId.CanyonEntrance,
                CanyonWorldLayout.CheckpointWorldPositions["canyon.v2.start"]);
            for (int i = 0; i < 2; i++) await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            level.Player.GlobalPosition = oldPlayer; level.Player.Velocity = Vector2.Zero;
            tree.Root.Size = oldSize; tree.Root.ContentScaleSize = oldScale;
            camera.ResetSmoothing(); camera.ForceUpdateScroll(); controller.ClearTransient();
        }
    }

    private static void VerifyActualPauseFreezesEveryTimer(SceneTree tree,
        PrologueLevelController level, PrologueGameFlowController flow,
        CanyonFireflyGuidanceController controller)
    {
        ProloguePauseMenu pause = level.GetNode<ProloguePauseMenu>("PauseMenu");
        Camera2D camera = level.Player.GetNode<Camera2D>("Camera2D");
        Vector2I oldSize = tree.Root.Size;
        Vector2I oldContentScale = tree.Root.ContentScaleSize;
        try
        {
            tree.Root.Size = new Vector2I(1920, 1080);
            tree.Root.ContentScaleSize = new Vector2I(1920, 1080);
            camera.Enabled = true;
            level.Player.GlobalPosition = new Vector2(1279f, 600f);
            level.Player.Velocity = Vector2.Zero;
            camera.ResetSmoothing();
            camera.ForceUpdateScroll();
            controller.ClearTransient();
            controller.RefreshNow(0f);
            TestAssert.True(controller.CurrentMarker != null,
                "actual pause fixture starts with a mounted on-screen route marker");

            controller.RefreshNow(CanyonFireflyGuidanceModel.StalledTriggerSeconds - 1f);
            TestAssert.True(!controller.CurrentFrame.Visible,
                "actual pause fixture primes one second below the stalled threshold");
            pause.Open();
            TestAssert.True(tree.Paused && flow.State == PrologueGameFlowState.Paused,
                "actual pause owner transitions both SceneTree and flow to Paused");
            CanyonFireflyFrame stalledBeforePause = controller.CurrentFrame;
            controller.RefreshNow(60f);
            TestAssert.Equal(stalledBeforePause, controller.CurrentFrame,
                "ordinary pause freezes accumulated stalled play time");
            pause.Execute(ProloguePauseCommand.Resume);
            TestAssert.True(!tree.Paused && flow.State == PrologueGameFlowState.Playing,
                "actual resume owner returns both SceneTree and flow to Playing");
            controller.RefreshNow(.99f);
            TestAssert.True(!controller.CurrentFrame.Visible,
                "resumed stalled clock remains below the exact threshold");
            controller.RefreshNow(.01f);
            TestAssert.True(controller.CurrentFrame.Visible,
                "resumed stalled clock reaches the exact threshold without pause reset");

            CanyonFireflyFrame visibleBeforePause = controller.CurrentFrame;
            pause.Open();
            controller.RefreshNow(60f);
            TestAssert.Equal(visibleBeforePause, controller.CurrentFrame,
                "ordinary pause freezes visible duration");
            pause.Execute(ProloguePauseCommand.Resume);
            controller.RefreshNow(CanyonFireflyGuidanceModel.VisibleDurationSeconds - .01f);
            TestAssert.True(controller.CurrentFrame.Visible,
                "resumed visible duration remains active immediately below expiry");
            controller.RefreshNow(.01f);
            TestAssert.True(!controller.CurrentFrame.Visible &&
                            Mathf.IsEqualApprox(CanyonFireflyGuidanceModel.CooldownDurationSeconds,
                                controller.CurrentFrame.CooldownSecondsRemaining),
                "visible expiry enters the full cooldown");

            CanyonFireflyFrame cooldownBeforePause = controller.CurrentFrame;
            pause.Open();
            controller.RefreshNow(60f);
            TestAssert.Equal(cooldownBeforePause, controller.CurrentFrame,
                "ordinary pause freezes cooldown duration");
            pause.Execute(ProloguePauseCommand.Resume);
            controller.RefreshNow(CanyonFireflyGuidanceModel.CooldownDurationSeconds - .01f);
            TestAssert.True(controller.CurrentFrame.CooldownSecondsRemaining > 0f,
                "resumed cooldown remains active immediately below expiry");
            controller.RefreshNow(.01f);
            TestAssert.True(Mathf.IsZeroApprox(controller.CurrentFrame.CooldownSecondsRemaining),
                "resumed cooldown expires only after all active-play seconds elapse");
            controller.ClearTransient();
        }
        finally
        {
            if (pause.IsOpen) pause.Execute(ProloguePauseCommand.Resume);
            tree.Root.Size = oldSize;
            tree.Root.ContentScaleSize = oldContentScale;
        }
    }

    private static void VerifyRemoteSameSectorMechanismDoesNotDefer(
        PrologueLevelController level, CanyonFireflyGuidanceController controller)
    {
        PrologueSector sector = level.CurrentSectorRoot;
        CanyonGearPiston nearby = sector.GetNode<CanyonGearPiston>(
            "Gameplay/Mechanisms/GearPiston");
        CanyonSteamJet remote = sector.GetNode<CanyonSteamJet>("Gameplay/Mechanisms/SteamJet");
        Camera2D camera = level.Player.GetNode<Camera2D>("Camera2D");
        camera.Enabled = true;
        level.Player.GlobalPosition = new Vector2(2600f, 3960f);
        level.Player.Velocity = Vector2.Zero;
        camera.ResetSmoothing();
        camera.ForceUpdateScroll();

        nearby.ResetForCheckpoint();
        remote.EnableForNewRun();
        remote.AdvanceForTest(1f);
        TestAssert.True(remote.IsDangerTelegraphActive && !nearby.IsDangerTelegraphActive,
            "actual remote SteamJet telegraphs while the nearby GearPiston is safe");
        controller.ClearTransient();
        controller.RefreshNow(0f);
        TestAssert.True(!controller.CombatTelegraphActive,
            "offscreen remote same-sector mechanism does not defer local route guidance");

        remote.DisableForRun();
        nearby.ResetForCheckpoint();
        nearby.AdvanceForTest(nearby.SafeSeconds + .01f);
        TestAssert.True(nearby.IsDangerTelegraphActive && !remote.IsDangerTelegraphActive,
            "actual nearby GearPiston telegraphs after the remote jet is disabled");
        controller.RefreshNow(0f);
        TestAssert.True(controller.CombatTelegraphActive,
            "nearby same-route visible mechanism defers local route guidance");
        nearby.ResetForCheckpoint();
        remote.EnableForNewRun();
        controller.ClearTransient();
    }

    private static async Task VerifyOutOfEncounterPublisherDoesNotDefer(SceneTree tree,
        PrologueLevelController level, CanyonFireflyGuidanceController controller)
    {
        Node actors = level.CurrentSectorRoot.GetNode<Node>("Gameplay/Actors");
        RustShellSquire rust = GD.Load<PackedScene>(
            "res://scenes/enemies/RustShellSquire.tscn").Instantiate<RustShellSquire>();
        actors.AddChild(rust);
        try
        {
            rust.Gravity = 0f;
            rust.GlobalPosition = new Vector2(1900f, 4300f);
            level.Player.GlobalPosition = rust.GlobalPosition + new Vector2(45f, 0f);
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            rust._PhysicsProcess(.01);
            TestAssert.Equal(AttackPhase.Telegraph, rust.CurrentFrame.AttackTimeline.Phase,
                "actual out-of-encounter Rust publisher enters Telegraph");
            TestAssert.True(CanyonCombatTelegraphAdapter.IsTelegraphActive(rust),
                "actual out-of-encounter publisher is independently observable");
            controller.RefreshNow(0f);
            TestAssert.True(!controller.CombatTelegraphActive,
                "world guidance ignores a telegraph outside every activated local encounter");
        }
        finally
        {
            actors.RemoveChild(rust);
            rust.Free();
        }
    }

    private static async Task VerifyLifecycleCleanupAndReentry(SceneTree tree,
        PrologueLevelController level, PrologueGameFlowController flow,
        SeamlessPrologueWorld world, CanyonFireflyGuidanceController controller,
        Marker2D safeExit)
    {
        Camera2D camera = level.Player.GetNode<Camera2D>("Camera2D");
        camera.Enabled = true;
        level.Player.GlobalPosition = new Vector2(7040f, 1080f);
        level.Player.Velocity = Vector2.Zero;
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        controller.RefreshNow(0f);
        TestAssert.True(controller.CurrentMarker == safeExit && controller.CurrentFrame.Visible &&
                        controller.GetNode<CanyonFireflyGuidanceView>("View").Visible,
            "actual Boss-exit state immediately presents the mounted reachable firefly");

        level.RequestRespawn("TASK8 LIFECYCLE RECOVERY");
        controller.RefreshNow(0f);
        TestAssert.Equal(PrologueGameFlowState.Recovering, flow.State,
            "actual recovery enters the transient flow state");
        AssertGuidanceCleared(controller, "recovery request");
        for (int i = 0; i < 3; i++)
        {
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
        }
        TestAssert.Equal(PrologueGameFlowState.Playing, flow.State,
            "actual recovery returns to active play");

        TestAssert.True(world.RestoreCheckpoint(PrologueSectorId.LostTemple,
                CanyonWorldLayout.CheckpointWorldPositions["canyon.v2.temple-forecourt"]),
            "lifecycle fixture remounts Temple after recovery");
        for (int i = 0; i < 2; i++)
        {
            await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        }
        level.Player.GlobalPosition = new Vector2(7040f, 1080f);
        camera.ResetSmoothing();
        await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        controller.RefreshNow(0f);
        TestAssert.True(controller.CurrentFrame.Visible,
            $"actual firefly can re-prime after recovery without retaining stale timers; " +
            $"sector={level.CurrentSector} marker={controller.CurrentMarker?.Name} " +
            $"player={level.Player.GlobalPosition} flow={flow.State}");

        flow.ReturnToTitle();
        controller.RefreshNow(0f);
        TestAssert.Equal(PrologueGameFlowState.Title, flow.State,
            "actual title transition succeeds from Playing");
        AssertGuidanceCleared(controller, "return to title");

        flow.StartNew();
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        controller.RefreshNow(0f);
        TestAssert.Equal(PrologueGameFlowState.Playing, flow.State,
            "actual new-run transition resumes Playing");
        AssertGuidanceCleared(controller, "new run");

        TestAssert.True(world.RestoreCheckpoint(PrologueSectorId.LostTemple,
                CanyonWorldLayout.CheckpointWorldPositions["canyon.v2.temple-forecourt"]),
            "completion fixture mounts Temple through the actual world");
        for (int i = 0; i < 2; i++)
        {
            await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        }
        level.Progress.ActivateMechanism(CanyonWorldLayout.CourtyardSealMechanismId);
        level.Progress.MarkBossDefeated();
        level.Player.GlobalPosition = new Vector2(7040f, 1080f);
        camera.ResetSmoothing();
        await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        controller.RefreshNow(0f);
        TestAssert.True(controller.CurrentFrame.Visible,
            "actual Boss-exit firefly re-primes in the new lifecycle");
        flow.Complete();
        controller.RefreshNow(0f);
        TestAssert.Equal(PrologueGameFlowState.Completed, flow.State,
            "actual completion enters the terminal state");
        AssertGuidanceCleared(controller, "completion");
    }

    private static void AssertGuidanceCleared(CanyonFireflyGuidanceController controller,
        string stage)
    {
        TestAssert.True(!controller.CurrentFrame.Visible && controller.CurrentMarker == null &&
                        !controller.CombatTelegraphActive &&
                        !controller.GetNode<CanyonFireflyGuidanceView>("View").Visible,
            $"firefly guidance clears model, target, telegraph and view on {stage}");
    }

    private static async Task VerifyLayout1SampleWhileParentResourcesAreLive(SceneTree tree)
    {
        string sampleRoot = Path.Combine(Path.GetTempPath(),
            "knight-task8-sample-" + Guid.NewGuid().ToString("N"));
        BasinQualitySampleEntry sample = GD.Load<PackedScene>(
            "res://scenes/prologue/sample/BasinQualitySample.tscn").Instantiate<BasinQualitySampleEntry>();
        sample.StorageDirectory = sampleRoot;
        tree.Root.AddChild(sample);
        try
        {
            for (int i = 0; i < 4; i++) await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            var sampleWorld = sample.Level.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors");
            var sampleController = sample.Level.GetNode<CanyonFireflyGuidanceController>(
                "WorldRoot/FireflyGuidance");
            TestAssert.True(sampleWorld.LayoutVersion == 1 && !sampleController.IsLayoutActive &&
                            !sampleController.CurrentFrame.Visible,
                "actual preserved layout1 sample keeps the new graph firefly inert");
            TestAssert.True(sample.Level.GetNodeOrNull("SampleGuide") != null,
                "layout1 sample retains its existing sample guidance");
        }
        finally
        {
            await TestSceneTeardown.FreeSceneAfterAudioDrain(tree, sample);
            if (Directory.Exists(sampleRoot)) Directory.Delete(sampleRoot, true);
        }
    }
}
