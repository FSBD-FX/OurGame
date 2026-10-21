using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;
using Knight.Abilities;
using Knight.Boss;
using Knight.Combat;
using Knight.Mechanisms;
using Knight.Player;
using Knight.Prologue;
using Knight.Prologue.Collections;
using Knight.Prologue.Seamless;
using Knight.Save;
using Knight.Shared;
using Knight.Tests.Presentation;
using Knight.Ui;

namespace Knight.Tests.Integration;

public static partial class PrologueCanyonGeneratedMainRouteTests
{
    private const string NaturalGearId = "canyon.v2.fragment.gear-gallery";
    private const string NaturalTempleId = "canyon.v2.fragment.temple-upper";

    private sealed class NaturalFragmentRouteContext
    {
        internal NaturalFragmentReceipt Receipt;
        internal bool GearOmitted;

        internal void AssertGearOmitted(PrologueLevelController level, SeamlessPrologueWorld world,
            CharacterBody2D player)
        {
            TestAssert.True(world.TryGetMountedSector(PrologueSectorId.CanyonBasin, out var basin),
                "Natural five-to-six: original basin remains mounted");
            var gear = basin.GetNode<QuestFragment>("Gameplay/QuestFragments/gear-gallery");
            bool collected = gear.IsCollected;
            if (collected)
                GD.Print($"NATURAL_FRAGMENT_ROUTE_PREREQUISITE scenario=natural-five-to-six gear_collected={collected} title_started=True synthetic_fixture=False");
            TestAssert.True(!collected,
                "Natural five-to-six: original Main driver must leave gear-gallery uncollected");
            TestAssert.True(level.Player == player && !level.Progress.CollectedFragmentIds.Contains(NaturalGearId) &&
                gear.FeedbackCount == 0 && !gear.GetOverlappingBodies().Contains(player),
                "Natural five-to-six: original gear is genuinely missed, not reset or reseeded");
            GearOmitted = true;
            GD.Print($"NATURAL_FRAGMENT_ROUTE_SKIPPED_GEAR player={player.GetInstanceId()} physics={Engine.GetPhysicsFrames()}");
        }
    }

    private sealed record NaturalFragmentReceipt(ulong PlayerId, ulong BossId, ulong BlockedPhysics,
        ulong CollectionPhysics, ulong ReturnedPhysics, ulong GateOpenVerifiedPhysics,
        int EntryCount, int ExitCount, int BodyEntries, int CollectedEvents, int SaveReplacements,
        int BoostEvents, int ReleaseEvents, string CollectionDiskHash,
        int EntryHp, int ExitHp, ulong EntryNextCast, ulong ExitNextCast,
        int Jumps, int Dashes, int Requests, int Recoveries, int ChainApplications,
        string EntryIds, string ExitIds, string SnapshotHash);

    internal static async Task<string> RunNaturalFiveToSix(SceneTree tree)
    {
        var context = new NaturalFragmentRouteContext();
        PrologueContinuousRouteResult route = await RunTask10(tree, Task10RouteVariant.Main,
            gearEntryWindowOnly: false, naturalRoute: context);
        route.AssertComplete("Main");
        NaturalFragmentReceipt r = context.Receipt;
        TestAssert.True(context.GearOmitted && r != null && r.PlayerId == route.PlayerInstanceId &&
            r.BossId == route.TerminalProof.BossInstanceId && r.EntryCount == 5 && r.ExitCount == 6 &&
            r.BlockedPhysics < r.CollectionPhysics && r.CollectionPhysics < r.ReturnedPhysics &&
            r.ReturnedPhysics < r.GateOpenVerifiedPhysics && r.GateOpenVerifiedPhysics < route.TerminalProof.ExposurePhysics &&
            r.BodyEntries == 1 && r.CollectedEvents == 1 && r.SaveReplacements == 1 &&
            r.BoostEvents == 1 && r.ReleaseEvents == 1 && r.EntryHp == r.ExitHp &&
            r.Requests == 0 && r.Recoveries == 0 && route.RecoveryRequests == 0 && route.RecoveryCompletions == 0,
            "Natural five-to-six: independent natural omission/return transaction precedes full physical terminal");
        string root = Task10RouteProofContract.ValidateRawStorageRoot(OS.GetCmdlineUserArgs()
            .Single(v => v.StartsWith("--case-storage-root=", StringComparison.Ordinal))["--case-storage-root=".Length..]);
        string path = Path.Combine(root, "quest-fragment-natural-route-result.json");
        for (string ancestor = Path.GetFullPath(path); ancestor != null; ancestor = Path.GetDirectoryName(ancestor))
            if ((File.Exists(ancestor) || Directory.Exists(ancestor)) &&
                (File.GetAttributes(ancestor) & FileAttributes.ReparsePoint) != 0)
                throw new ArgumentException("natural route rejects reparse output ancestors");
        using JsonDocument main = JsonDocument.Parse(Task10RouteResultArtifact.ToJson(route, "Main", syntheticFixture: false));
        byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(new
        {
            version = 1, scenario = "natural-five-to-six", proofKind = "automated", syntheticFixture = false,
            humanValidated = false, operationCoverageComplete = false, fullGoalAccepted = false,
            receipt = r, mainDriver = main.RootElement.Clone()
        }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true });
        using (var stream = new FileStream(path, FileMode.CreateNew, System.IO.FileAccess.Write, FileShare.None))
        { stream.Write(bytes); stream.Flush(flushToDisk: true); }
        TestAssert.True(File.ReadAllBytes(path).SequenceEqual(bytes), "Natural five-to-six: exclusive result re-read matches");
        return path;
    }

    private static async Task SkipNaturalGearFragment(SceneTree tree, PrologueLevelController level,
        SeamlessPrologueWorld world, CharacterBody2D player, RouteMonitor monitor,
        Action observe, Func<Action, Action> swapGuard)
    {
        void Check(bool value, string message) => TestAssert.True(value, "Natural gear omission: " + message);
        bool Live(Node n) => n != null && GodotObject.IsInstanceValid(n) && n.IsInsideTree() && !n.IsQueuedForDeletion();
        Check(world.TryGetMountedSector(PrologueSectorId.CanyonBasin, out var basin), "original basin remains mounted");
        var gear = basin.GetNode<QuestFragment>("Gameplay/QuestFragments/gear-gallery");
        var gearShape = gear.GetNode<CollisionShape2D>("Collision");
        var one = basin.GetNode<StaticBody2D>("Gameplay/Collision/UpperGearGalleryOne");
        var two = basin.GetNode<StaticBody2D>("Gameplay/Collision/UpperGearGalleryTwo");
        var health = player.GetNode<HealthComponent>("Health");
        var motor = player.GetNode<PlayerMotor>("Motor");
        var input = player.GetNode<PlayerInputReader>("Input");
        var claw = player.GetNode<MechanicalClawController>("MechanicalClaw");
        var shape = player.GetNode<CollisionShape2D>("BodyCollider");
        var flow = level.GetNode<PrologueGameFlowController>("GameFlow");
        var keyboard = KeyboardRouteInputDriver.Current;
        Node[] nodes = { level, world, basin, player, gear, gearShape, one, two, health, motor, input, claw, shape, flow };
        Check(nodes.All(Live) && keyboard != null && shape.Shape is CapsuleShape2D &&
            gearShape.Shape is CircleShape2D { Radius: 18 }, "native original supported gallery and keyboard/capsule/fragment");
        ulong[] ids = nodes.Select(n => n.GetInstanceId()).ToArray();
        var circle = (CircleShape2D)gearShape.Shape; var capsule = (CapsuleShape2D)shape.Shape;
        int hp = health.CurrentHealth, jumps = motor.AcceptedJumpCount, dashes = motor.AcceptedDashCount;
        int requests = monitor.RecoveryRequested, recoveries = monitor.RecoveryCompleted;
        int k = keyboard.ClawEdges, j = keyboard.AttackEdges, feedback = gear.FeedbackCount;
        ulong nextCast = claw.CurrentFrame.NextCastId;
        string collectedIds = Task18Ids(level.Progress.CollectedFragmentIds);
        FragmentCollectionResult lastResult = gear.LastResult;
        bool issued = false, secondJump = false;
        Action previousGuard = null;
        void Guard()
        {
            Check(nodes.All(Live) && nodes.Select(n => n.GetInstanceId()).SequenceEqual(ids) &&
                GodotObject.IsInstanceValid(circle) && GodotObject.IsInstanceValid(capsule) &&
                gearShape.Shape == circle && shape.Shape == capsule && !gearShape.Disabled && !shape.Disabled &&
                gear.Monitoring && gear.CollisionMask == 2 && circle.Radius == 18 &&
                gear.GlobalPosition == new Vector2(3160, 3720), "retains enabled original native gear and body geometry");
            float radius = capsule.Radius * Mathf.Abs(shape.GlobalTransform.X.X);
            float spine = capsule.Height * .5f * Mathf.Abs(shape.GlobalTransform.Y.Y) - radius;
            Vector2 body = shape.GlobalPosition, center = gearShape.GlobalPosition;
            Vector2 nearest = new(body.X, Mathf.Clamp(center.Y, body.Y - spine, body.Y + spine));
            Check(!gear.IsCollected && !gear.GetOverlappingBodies().Contains(player) && nearest.DistanceTo(center) > radius + circle.Radius &&
                gear.FeedbackCount == feedback && gear.LastResult == lastResult &&
                Task18Ids(level.Progress.CollectedFragmentIds) == collectedIds && level.Player == player &&
                monitor.Player == player && flow.State == PrologueGameFlowState.Playing && !tree.Paused &&
                health.CurrentHealth == hp && monitor.RecoveryRequested == requests && monitor.RecoveryCompleted == recoveries &&
                motor.AcceptedDashCount == dashes && motor.AcceptedJumpCount >= jumps &&
                motor.AcceptedJumpCount <= jumps + (issued ? secondJump ? 2 : 1 : 0) &&
                keyboard.ClawEdges == k && keyboard.AttackEdges == j && claw.CurrentFrame.NextCastId == nextCast &&
                claw.CurrentFrame.State == MechanicalClawState.Idle &&
                input.CurrentFrame.KeyboardActions,
                "normal jump never touches/consumes gear, adds HP loss, recovery, attack, dash or claw");
            previousGuard?.Invoke();
        }
        async Task Tick()
        {
            Guard(); await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame); Guard(); observe();
        }
        previousGuard = swapGuard(Guard);
        try
        {
            ReleaseAll(player.GlobalPosition);
            for (int i = 0; i < 8 && Mathf.Abs(player.Velocity.X) > 1; i++) await Tick();
            Check(HasFloorContact(player, one) && player.GlobalPosition.X >= 3080 && player.GlobalPosition.X <= 3100 &&
                Mathf.Abs(player.GlobalPosition.Y - 3720) < 1 && Mathf.Abs(player.Velocity.X) <= 1,
                "original GalleryOne west landing settles in bounded safe launch interval");
            issued = true; SendAction(InputActions.MoveRight, true); SendAction(InputActions.Jump, true);
            bool departed = false, landed = false, rightReleased = false;
            for (int i = 0; i < 60 && !landed; i++)
            {
                await Tick(); departed |= !player.IsOnFloor();
                if (!rightReleased && player.GlobalPosition.X >= 3210)
                { SendAction(InputActions.MoveRight, false); rightReleased = true; }
                landed = departed && HasFloorContact(player, one);
            }
            SendAction(InputActions.Jump, false); SendAction(InputActions.MoveRight, false);
            for (int i = 0; i < 8 && Mathf.Abs(player.Velocity.X) > 1; i++) await Tick();
            Check(landed && rightReleased && HasFloorContact(player, one) &&
                player.GlobalPosition.X > 3198 && player.GlobalPosition.X <= 3260 &&
                Mathf.Abs(player.Velocity.X) <= 1 && motor.AcceptedJumpCount == jumps + 1,
                "one full-height Space jump lands wholly east of gear on same original support");
            secondJump = true;
            await PrologueCanyonTraversalTests.Task4ReachSupport(tree, player, two, clawRequired: false);
            Guard();
            Check(HasFloorContact(player, two) && motor.AcceptedJumpCount == jumps + 2,
                "unchanged next gallery helper departs east side without walking back through gear");
            GD.Print($"NATURAL_FRAGMENT_GEAR_JUMP_OK player={player.GetInstanceId()} physics={Engine.GetPhysicsFrames()} jumps={jumps}->{motor.AcceptedJumpCount} position={player.GlobalPosition}");
        }
        finally { ReleaseAll(Live(player) ? player.GlobalPosition : Vector2.Zero); swapGuard(previousGuard); }
    }

    // This independent scenario starts at the real first Temple checkpoint after
    // the complete Main combat route. It never assembles a fixture, seeds progress,
    // moves a body by assignment, bypasses a save, heals, or consumes the supply.
    private static async Task<NaturalFragmentReceipt> DriveNaturalFragmentReturn(SceneTree tree,
        PrologueLevelController level, SeamlessPrologueWorld world, CharacterBody2D player,
        RouteMonitor monitor, Action observe, Func<Action, Action> swapGuard,
        Func<Action, Action> swapPre, string snapshotPath)
    {
        void Check(bool value, string message) => TestAssert.True(value, "Natural five-to-six: " + message);
        bool Live(Node node) => node != null && GodotObject.IsInstanceValid(node) &&
            node.IsInsideTree() && !node.IsQueuedForDeletion();
        Check(world.TryGetMountedSector(PrologueSectorId.LostTemple, out var temple), "actual Temple mounted");
        var flow = level.GetNode<PrologueGameFlowController>("GameFlow");
        var health = player.GetNode<HealthComponent>("Health");
        var input = player.GetNode<PlayerInputReader>("Input");
        var motor = player.GetNode<PlayerMotor>("Motor");
        var claw = player.GetNode<MechanicalClawController>("MechanicalClaw");
        var modes = player.GetNode<PlayerActionModeController>("ActionModes");
        var bodyShape = player.GetNode<CollisionShape2D>("BodyCollider");
        var floor = temple.GetNode<StaticBody2D>("Gameplay/Collision/CheckpointFloor");
        var arenaFloor = temple.GetNode<StaticBody2D>("Gameplay/Collision/ArenaFloor");
        var shelf = temple.GetNode<StaticBody2D>("Gameplay/Collision/TempleSupplyShelf");
        var floorShape = floor.GetNode<CollisionShape2D>("Collision");
        var arenaFloorShape = arenaFloor.GetNode<CollisionShape2D>("Collision");
        var shelfShape = shelf.GetNode<CollisionShape2D>("Collision");
        var beam = temple.GetNode<GrappleSurface2D>("Gameplay/Collision/TempleSupplyGripBeam/GrappleSurfaceProfile");
        var fragment = temple.GetNode<QuestFragment>("Gameplay/QuestFragments/temple-upper");
        var fragmentShape = fragment.GetNode<CollisionShape2D>("Collision");
        var visual = fragment.GetNode<Node2D>("AvailableVisual");
        var hud = level.GetNode<PrologueFormalHud>("Hud/FormalHud");
        var arena = temple.GetNode<BossArenaController>("Gameplay/BossArenaController");
        var boss = arena.CurrentBoss;
        Check(Live(boss), "actual original Boss exists before reading health");
        var bossHealth = boss.GetNode<HealthComponent>("Health");
        var trigger = arena.GetNode<Area2D>(arena.ActivationTriggerPath);
        var barrier = arena.GetNode<CollisionShape2D>(arena.AdmissionBarrierCollisionPath).GetParent<StaticBody2D>();
        var barriers = barrier.GetChildren().OfType<CollisionShape2D>().ToArray();
        var chain = temple.GetNode<BellChainMechanism>("Gameplay/Mechanisms/BellChain");
        var chainShape = chain.GetNode<CollisionShape2D>("Door/Collision");
        var adapter = chain.GetNode<LegacyThreadInteractionAdapter>("ThreadTarget/LegacyThreadInteractionAdapter");
        var reward = temple.GetNode<PrologueSupplyCache>("Gameplay/Rewards/TempleSupply");
        var keyboard = KeyboardRouteInputDriver.Current;
        Node[] nodes = { level, world, temple, player, monitor.Player, flow, health, input, motor, claw, modes,
            bodyShape, floor, arenaFloor, shelf, floorShape, arenaFloorShape, shelfShape, beam, fragment,
            fragmentShape, visual, hud, arena, boss, bossHealth, trigger, barrier, chain, chainShape, adapter, reward };
        Check(nodes.All(Live) && barriers.Length == 3 && barriers.All(Live) && keyboard != null,
            "original live same-instance nodes, all three hull faces and real keyboard driver");
        ulong[] identities = nodes.Select(n => n.GetInstanceId()).ToArray();
        CollisionShape2D[] shapes = new[] { bodyShape, floorShape, arenaFloorShape, shelfShape, fragmentShape, chainShape }
            .Concat(barriers).ToArray();
        Check(shapes.All(s => GodotObject.IsInstanceValid(s.Shape)) &&
            bodyShape.Shape is CapsuleShape2D && fragmentShape.Shape is CircleShape2D { Radius: 18 },
            "retains authored capsule and circular native fragment");
        Resource[] resources = shapes.Select(s => (Resource)s.Shape).ToArray();
        Transform2D fragmentTransform = fragmentShape.GlobalTransform;
        byte[] snapshot = File.ReadAllBytes(snapshotPath);
        string snapshotHash = Convert.ToHexString(SHA256.HashData(snapshot));
        PrologueSaveData baseline = Task18RawSave(snapshot), initialSave = flow.Save, acceptedSave = initialSave;
        string entryIds = Task18Ids(new[] { "entrance", "rift", "abyss-floor", "ascent-mid", "courtyard" }
            .Select(id => "canyon.v2.fragment." + id));
        string exitIds = Task18Ids(baseline.CollectedFragmentIds.Append(NaturalTempleId));
        string discoveries = Task18Ids(baseline.Discoveries), savePath = flow.SavePath;
        Check(baseline.CollectedFragmentIds.Count == 5 && Task18Ids(baseline.CollectedFragmentIds) == entryIds &&
            !baseline.LegacyCollectionQuotaExempt && !fragment.IsCollected && fragment.StableId == NaturalTempleId &&
            fragment.GlobalPosition == new Vector2(5896, 924) && fragment.FeedbackCount == 0 && visual.Visible &&
            Task18SaveExact(initialSave, baseline, entryIds, discoveries) &&
            snapshot.SequenceEqual(File.ReadAllBytes(savePath)) && !reward.IsSpent,
            "natural exact five arrive with no quota exemption, no supply and original uncollected sixth candidate");
        int hp = health.CurrentHealth, maxHp = health.MaxHealth, jumps = motor.AcceptedJumpCount;
        int dashes = motor.AcceptedDashCount, requests = monitor.RecoveryRequested, recoveries = monitor.RecoveryCompleted;
        int applications = adapter.ApplicationCount, bossHp = bossHealth.CurrentHealth;
        int attackEdges = keyboard.AttackEdges, clawEdges = keyboard.ClawEdges;
        ulong playerId = player.GetInstanceId(), bossId = boss.GetInstanceId(), nextCast = claw.CurrentFrame.NextCastId;
        int bodies = 0, collected = 0, replacements = 0, boosts = 0, releases = 0, casts = 0, spaces = 0;
        ulong blockedTick = 0, collectedTick = 0, returnedTick = 0, openedTick = 0, arrivalTick = 0;
        ulong castSequence = 0, spaceSequence = 0;
        string diskHash = string.Empty, phase = "blocked-approach";
        bool castIssued = false, spaceIssued = false, travel = false, latched = false, arrived = false, idle = false;
        bool aimHeld = false, clawHeld = false;
        Exception callbackFailure = null;
        Action previousGuard = null, previousPre = null;
        bool guardInstalled = false, preInstalled = false, bodySubscribed = false, collectionSubscribed = false, clawSubscribed = false;

        void Native()
        {
            Check(GodotObject.IsInstanceValid(tree) && nodes.All(Live) && barriers.All(Live) &&
                nodes.Select(n => n.GetInstanceId()).SequenceEqual(identities) &&
                shapes.Select(s => s.Shape).SequenceEqual(resources), "native identities/resources remain unchanged");
            Check(fragmentShape.GlobalTransform == fragmentTransform && fragment.GlobalPosition == new Vector2(5896, 924) &&
                fragmentShape.Shape is CircleShape2D { Radius: 18 } && !fragmentShape.Disabled &&
                fragment.Monitoring && fragment.CollisionMask == 2, "fragment area remains original and enabled");
        }
        bool Idle() => claw.CurrentFrame.State == MechanicalClawState.Idle && claw.CurrentFrame.CastId == 0 &&
            claw.CurrentTarget == null && claw.ResidencyTarget == null && claw.CurrentFrame.LockedTargetRuntimeId == 0;
        Rect2 Bounds()
        {
            var capsule = (CapsuleShape2D)bodyShape.Shape;
            Check(!bodyShape.Disabled && Mathf.Abs(bodyShape.GlobalTransform.X.Y) < .001f &&
                Mathf.Abs(bodyShape.GlobalTransform.Y.X) < .001f, "upright enabled actual capsule");
            Vector2 half = new(capsule.Radius * Mathf.Abs(bodyShape.GlobalTransform.X.X),
                capsule.Height * .5f * Mathf.Abs(bodyShape.GlobalTransform.Y.Y));
            return new Rect2(bodyShape.GlobalPosition - half, half * 2);
        }
        bool Supported(StaticBody2D support, CollisionShape2D shape)
        {
            if (shape.Disabled || shape.Shape is not RectangleShape2D rect || !player.IsOnFloor()) return false;
            Rect2 b = Bounds();
            float top = shape.ToGlobal(new Vector2(0, -rect.Size.Y / 2)).Y;
            if (b.Position.X < shape.ToGlobal(new Vector2(-rect.Size.X / 2, 0)).X ||
                b.End.X > shape.ToGlobal(new Vector2(rect.Size.X / 2, 0)).X ||
                Mathf.Abs(b.End.Y - top) > 1 || Mathf.Abs(player.Velocity.Y) > .01f) return false;
            foreach (float x in new[] { b.GetCenter().X, b.Position.X + 1, b.End.X - 1 })
            {
                using var q = PhysicsRayQueryParameters2D.Create(new(x, b.End.Y - .25f), new(x, b.End.Y + 1), CombatCollisionLayers.World);
                q.CollideWithAreas = false;
                using var hit = player.GetWorld2D().DirectSpaceState.IntersectRay(q);
                if (hit.Count == 0 || hit["collider"].AsGodotObject() != support || hit["normal"].AsVector2().Y > -.99f) return false;
            }
            return true;
        }
        bool FragmentOverlap()
        {
            Rect2 b = Bounds(); float radius = b.Size.X / 2, spine = b.Size.Y / 2 - radius;
            Vector2 c = b.GetCenter(); Vector2 nearest = new(c.X, Mathf.Clamp(fragmentTransform.Origin.Y, c.Y - spine, c.Y + spine));
            return fragment.GetOverlappingBodies().Contains(player) && nearest.DistanceTo(fragmentTransform.Origin) <= radius + 18;
        }
        void Disk()
        {
            string ids = collected == 0 ? entryIds : exitIds;
            Check(collected != 0 || barriers.All(s => !s.Disabled),
                "all three admission hull faces stay closed on every five-fragment sample");
            Check(snapshot.SequenceEqual(File.ReadAllBytes(snapshotPath)) &&
                Task18SaveExact(Task18RawSave(File.ReadAllBytes(savePath)), baseline, ids, discoveries) &&
                Task18SaveExact(flow.Save, baseline, ids, discoveries) &&
                Task18SaveExact(initialSave, baseline, entryIds, discoveries),
                "all twenty raw disk/live/initial snapshot values preserve sole collected-ID delta");
        }
        void Safety()
        {
            Native();
            if (callbackFailure != null) throw new InvalidOperationException("Natural fragment callback failure", callbackFailure);
            Check(collected != 0 || barriers.All(s => !s.Disabled),
                "all three hull faces remain closed on every pre-collection sample");
            Check(level.Player == player && monitor.Player == player && arena.CurrentBoss == boss &&
                flow.State == PrologueGameFlowState.Playing && !tree.Paused && health.CurrentHealth == hp && hp > 0 &&
                health.MaxHealth == maxHp && monitor.RecoveryRequested == requests && monitor.RecoveryCompleted == recoveries &&
                motor.AcceptedJumpCount == jumps && motor.AcceptedDashCount == dashes &&
                !arena.IsEncounterActive && !level.Progress.BossDefeated && !trigger.GetOverlappingBodies().Contains(player) &&
                bossHealth.CurrentHealth == bossHp && bossHp == bossHealth.MaxHealth &&
                player.GlobalPosition.IsFinite() && player.Velocity.IsFinite(), "same living player/no recovery/jump/dash or premature Boss damage/activation");
            var checkpoint = level.Progress.RestoreCheckpoint();
            Check(checkpoint.HasActivatedCheckpoint && checkpoint.CheckpointId == "canyon.v2.temple-forecourt" &&
                checkpoint.SectorId == PrologueSectorId.LostTemple && checkpoint.WorldPosition == new Vector2(5920, 1080) &&
                world.CurrentSector == PrologueSectorId.LostTemple && world.CheckpointId == checkpoint.CheckpointId &&
                level.CurrentSectorRoot == temple && chain.IsOpen && chainShape.Disabled &&
                adapter.ApplicationCount == applications && level.Progress.BellChainShortcutOpen &&
                level.Progress.ActivatedMechanisms.Contains(CanyonWorldLayout.CourtyardSealMechanismId) && !reward.IsSpent,
                "original seal, bell and checkpoint persist; no supply reward consumed");
            string ids = collected == 0 ? entryIds : exitIds;
            Check(collected is 0 or 1 && bodies is 0 or 1 && replacements == collected &&
                ReferenceEquals(flow.Save, acceptedSave) && flow.SavePath == savePath &&
                Task18Ids(level.Progress.CollectedFragmentIds) == ids && Task18Ids(level.Progress.Discoveries) == discoveries &&
                Task18Ids(level.Progress.OpenShortcuts) == Task18Ids(baseline.OpenShortcuts) &&
                Task18Ids(level.Progress.ActivatedMechanisms) == Task18Ids(baseline.ActivatedMechanisms) &&
                Task18Ids(level.Progress.CompletedTutorials.Select(s => s.ToString())) == Task18Ids(baseline.CompletedTutorials) &&
                !level.Progress.LegacyCollectionQuotaExempt && level.Progress.CollectionRuleVersion == baseline.CollectionRuleVersion &&
                Task18SaveExact(flow.Save, baseline, ids, discoveries) && fragment.IsCollected == (collected == 1) &&
                fragment.FeedbackCount == collected && visual.Visible == (collected == 0) &&
                hud.CurrentModel.FragmentCount == 5 + collected && hud.CurrentModel.FragmentRequired == 6 && !hud.CurrentModel.CollectionQuotaExempt,
                "sole verified fragment transaction updates IDs, Save and HUD exactly once");
            Check(input.CurrentFrame.KeyboardActions &&
                !input.CurrentFrame.PrimaryPressed && !input.CurrentFrame.PrimaryHeld && !input.CurrentFrame.PrimaryReleased &&
                !input.CurrentFrame.GrappleModePressed && !input.CurrentFrame.AttackModePressed && !input.CurrentFrame.LegacyShieldHeld &&
                !input.CurrentFrame.AttackPressed && !input.CurrentFrame.InteractPressed && !input.CurrentFrame.DashPressed &&
                !input.CurrentFrame.ShieldHeld && (!(input.CurrentFrame.JumpPressed || input.CurrentFrame.JumpHeld) || spaceIssued) &&
                !Input.IsPhysicalKeyPressed(Key.J) && !Input.IsPhysicalKeyPressed(Key.Q) && !Input.IsPhysicalKeyPressed(Key.E) &&
                !Input.IsPhysicalKeyPressed(Key.Shift) && !Input.IsPhysicalKeyPressed(Key.L) &&
                !Input.IsActionPressed(InputActions.PrimaryAction) && !Input.IsActionPressed(InputActions.SelectGrappleMode) &&
                !Input.IsActionPressed(InputActions.SelectAttackMode) && !Input.IsActionPressed(InputActions.Shield) &&
                !Input.IsActionPressed(InputActions.MoveDown) && (!Input.IsActionPressed(InputActions.MoveUp) || aimHeld) &&
                (!Input.IsActionPressed(InputActions.Claw) || clawHeld) && keyboard.AttackEdges == attackEdges &&
                keyboard.ClawEdges >= clawEdges && keyboard.ClawEdges <= clawEdges + (castIssued ? 1 : 0),
                "only real A/D, one owned W+K and one Space; no mouse, combat, interact, dash or shield");
            Check(claw.CurrentFrame.Failure == MechanicalClawFailure.None &&
                (claw.CurrentFrame.CastId == 0 || castIssued && claw.CurrentFrame.CastId == nextCast) &&
                (claw.CurrentFrame.NextCastId == nextCast || castIssued && claw.CurrentFrame.NextCastId == nextCast + 1),
                "one successful owned cast without retries");
        }
        void Guard()
        {
            Safety(); previousGuard?.Invoke();
            if (input.CurrentFrame.ClawPressed && castSequence != input.CurrentFrameSequence)
            { castSequence = input.CurrentFrameSequence; casts++; Check(castIssued && casts == 1, "one actual sampled K edge"); }
            if (input.CurrentFrame.JumpPressed && spaceSequence != input.CurrentFrameSequence)
            { spaceSequence = input.CurrentFrameSequence; spaces++; Check(spaceIssued && spaces == 1, "one actual sampled Space edge"); }
        }
        void Pre() { Safety(); previousPre?.Invoke(); }
        async Task Tick()
        {
            Safety(); await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
            Native(); await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            Safety(); observe();
        }
        async Task Walk(float target, bool precise = false)
        {
            SendAction(InputActions.MoveLeft, false); SendAction(InputActions.MoveRight, false);
            int direction = MathF.Sign(target - player.GlobalPosition.X);
            bool Ready() => Mathf.Abs(target - player.GlobalPosition.X) <= 1.5f && player.Velocity.Length() <= 2 &&
                input.CurrentFrame.MoveAxis == 0 && Supported(floor, floorShape);
            if (!precise) SendAction(direction > 0 ? InputActions.MoveRight : InputActions.MoveLeft, true);
            try
            {
                for (int i = 0; i < 120 && (precise ? !Ready() : (target - player.GlobalPosition.X) * direction > 2); i++)
                {
                    if (precise)
                    {
                        float delta = target - player.GlobalPosition.X, vx = player.Velocity.X;
                        float braking = vx * vx / (2 * motor.Tuning.GroundDeceleration) + Mathf.Abs(vx) / Engine.PhysicsTicksPerSecond;
                        int steer = Mathf.Abs(delta) <= 1.5f ? 0 : MathF.Sign(delta);
                        if (steer != 0 && vx * delta > 0 && Mathf.Abs(delta) <= braking + 1.5f) steer = 0;
                        SendAction(InputActions.MoveLeft, steer < 0); SendAction(InputActions.MoveRight, steer > 0);
                    }
                    await Tick();
                }
                Check(precise ? Ready() : (target - player.GlobalPosition.X) * direction <= 2, $"bounded ordinary walk to {target}");
            }
            finally { SendAction(InputActions.MoveLeft, false); SendAction(InputActions.MoveRight, false); }
            for (int i = 0; i < 6; i++) await Tick();
        }
        void Callback(Action action)
        { try { Native(); action(); } catch (Exception error) { callbackFailure = callbackFailure == null ? error : new AggregateException(callbackFailure, error); } }
        void OnCollected(string id) => Callback(() =>
        {
            collected++;
            Check(collected == 1 && phase == "shelf-landing" && id == NaturalTempleId && spaceIssued && boosts == 1 && releases == 1 &&
                FragmentOverlap() && fragment.IsCollected && fragment.FeedbackCount == 1 &&
                fragment.LastResult == FragmentCollectionResult.Collected && !visual.Visible &&
                !ReferenceEquals(flow.Save, initialSave) && replacements == 0 &&
                ReferenceEquals(acceptedSave, initialSave), "unique real Collected comes from native original capsule/area overlap");
            Disk(); acceptedSave = flow.Save; replacements++;
            collectedTick = Engine.GetPhysicsFrames();
            diskHash = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(savePath)));
            GD.Print($"NATURAL_FRAGMENT_ROUTE_COLLECTED count=6 player={playerId} physics={collectedTick} disk_sha256={diskHash}");
        });
        void OnBody(Node2D body) => Callback(() =>
        {
            bodies++;
            Check(bodies == 1 && body == player && body.GetInstanceId() == playerId && FragmentOverlap() &&
                collected == 1 && replacements == 1, "one real same-player BodyEntered follows the synchronous production consumer");
        });
        void OnClaw(MechanicalClawFrame frame, GrappleSurface2D target) => Callback(() =>
        {
            Check(frame.Failure == MechanicalClawFailure.None && frame.State != MechanicalClawState.Failed &&
                (frame.CastId == 0 || castIssued && frame.CastId == nextCast), "all claw publications retain sole successful cast");
            if (frame.State is MechanicalClawState.Travel or MechanicalClawState.Latched or MechanicalClawState.Reel or MechanicalClawState.Swing)
            {
                Check(target == beam && frame.LockedTargetRuntimeId == beam.GetInstanceId() &&
                    frame.AnchorPoint.X >= 5805 && frame.AnchorPoint.X <= 5855 && Mathf.Abs(frame.AnchorPoint.Y - 860) < 1 &&
                    frame.SurfaceNormal.Y > .7f, "cast stays on original beam underside");
                if (frame.State == MechanicalClawState.Travel) travel = true;
                if (frame.State == MechanicalClawState.Latched) { Check(travel, "Travel precedes Latched"); latched = true; }
                if (frame.Arrived && !arrived)
                { Check(latched && frame.State == MechanicalClawState.Swing && !spaceIssued, "first actual Arrived precedes Space"); arrived = true; arrivalTick = Engine.GetPhysicsFrames(); }
            }
            if (frame.State == MechanicalClawState.Boost)
            { boosts++; Check(spaceIssued && arrived && boosts == 1 && releases == 0 && frame.BoostConsumed && frame.ReleaseKind == MechanicalClawReleaseKind.Boost, "genuine one Space Boost"); }
            if (frame.State == MechanicalClawState.Released)
            { releases++; Check(boosts == 1 && releases == 1 && frame.ReleaseKind == MechanicalClawReleaseKind.Boost && frame.BoostConsumed, "genuine Boost release, not Cancel"); }
            if (frame.State == MechanicalClawState.Idle && castIssued)
            { Check(releases == 1 && frame.CastId == 0 && frame.NextCastId == nextCast + 1, "natural target-free Idle after release"); idle = true; }
        });
        try
        {
            previousGuard = swapGuard(Guard); guardInstalled = true;
            previousPre = swapPre(Pre); preInstalled = true;
            fragment.BodyEntered += OnBody; bodySubscribed = true;
            fragment.Collected += OnCollected; collectionSubscribed = true;
            claw.ClawFrameChanged += OnClaw; clawSubscribed = true;
            Safety(); Disk();
            Check(Idle() && Supported(floor, floorShape) && barriers.All(s => !s.Disabled) &&
                !BossAdmissionModel.Evaluate(level.Progress).Allowed, "five genuine IDs keep finite hull closed despite seal and bell");
            await Walk(6120);
            SendAction(InputActions.MoveRight, true);
            for (int i = 0; i < 20; i++) await Tick();
            SendAction(InputActions.MoveRight, false);
            Rect2 stopped = Bounds();
            // y900 is exclusively the admission hull; at y1000 the independent
            // Entry combat door shares the same left plane and can win the ray tie.
            using (var q = PhysicsRayQueryParameters2D.Create(new(6120, 900), new(6200, 900), CombatCollisionLayers.World))
            {
                q.CollideWithAreas = false;
                using var hit = player.GetWorld2D().DirectSpaceState.IntersectRay(q);
                GD.Print($"NATURAL_FRAGMENT_BLOCK_GEOMETRY player={playerId} position={player.GlobalPosition} " +
                    $"body={stopped} on_floor={player.IsOnFloor()} full_floor={Supported(arenaFloor, arenaFloorShape)} " +
                    $"ray_collider={(hit.Count > 0 ? hit["collider"].AsGodotObject()?.GetInstanceId() : 0)} barrier={barrier.GetInstanceId()}");
                Check(hit.Count > 0 && hit["collider"].AsGodotObject() == barrier && barriers.All(s => !s.Disabled) &&
                    barrier.ShapeOwnerGetOwner(barrier.ShapeFindOwner(hit["shape"].AsInt32())) ==
                        arena.GetNode<CollisionShape2D>(arena.AdmissionBarrierCollisionPath) &&
                    stopped.End.X <= 6142.1f && Mathf.Abs(player.GlobalPosition.X - 6122) <= 1 &&
                    Supported(arenaFloor, arenaFloorShape), "actual D is physically blocked by original left hull with complete supported capsule outside");
            }
            blockedTick = Engine.GetPhysicsFrames(); Disk();
            GD.Print($"NATURAL_FRAGMENT_ROUTE_BLOCKED count=5 player={playerId} physics={blockedTick} position={player.GlobalPosition}");
            phase = "return-to-beam"; await Walk(5920); await Walk(5825, precise: true);
            Check(Idle() && Supported(floor, floorShape), "ordinary leftward return reaches original beam stance");
            phase = "beam-prepare"; ulong prepareSequence = input.CurrentFrameSequence;
            aimHeld = true; SendAction(InputActions.MoveUp, true);
            for (int i = 0; i < 8 && (input.CurrentFrameSequence <= prepareSequence || input.KeyboardAimDirection.Dot(Vector2.Up) < .999f); i++) await Tick();
            var resolved = new MechanicalClawTargetResolver().ResolveKeyboard(player, input.KeyboardAimDirection);
            Check(Input.IsPhysicalKeyPressed(Key.W) && !Input.IsPhysicalKeyPressed(Key.K) && Idle() && modes.CanBeginGrapple &&
                resolved.Surface == beam && resolved.Snapshot.HasLineOfSight && !resolved.Snapshot.IsLegacyInteraction &&
                IsTargetVisible(player, resolved.Snapshot.WorldAnchor) &&
                player.GlobalPosition.DistanceTo(resolved.Snapshot.WorldAnchor) <= MechanicalClawSettings.Default.MaxRange,
                "fresh physical W resolves actual visible in-range LOS nonlegacy beam");
            phase = "beam-cast"; castIssued = true; clawHeld = true; SendAction(InputActions.Claw, true);
            int castWaits = 0;
            try
            {
                for (; castWaits < 8; castWaits++)
                { await Tick(); if (casts == 1 && claw.CurrentFrame.State is MechanicalClawState.Aim or MechanicalClawState.Windup or MechanicalClawState.Travel) break; }
                Check(casts == 1 && claw.CurrentTarget == beam && Input.IsPhysicalKeyPressed(Key.K) &&
                    Input.IsPhysicalKeyPressed(Key.W), "one W+K admitted before releasing direction");
            }
            finally { SendAction(InputActions.Claw, false); clawHeld = false; SendAction(InputActions.MoveUp, false); aimHeld = false; }
            for (; castWaits < 300 && !arrived; castWaits++) await Tick();
            Check(travel && latched && arrived && claw.CurrentFrame.Arrived && claw.CurrentFrame.State == MechanicalClawState.Swing &&
                arrivalTick == Engine.GetPhysicsFrames(), "first actual post-motor Arrived within finite300 precedes release");
            phase = "beam-release"; spaceIssued = true;
            SendAction(InputActions.MoveRight, true); SendAction(InputActions.Jump, true);
            for (int i = 0; i < 3; i++) await Tick();
            SendAction(InputActions.Jump, false);
            Check(boosts == 1 && releases == 1 && spaces == 1, "same-batch D+Space makes one Boost, no ordinary jump");
            phase = "shelf-landing"; await Walk(5896);
            for (int i = 0; i < 90 && !Supported(shelf, shelfShape); i++) await Tick();
            Check(Supported(shelf, shelfShape) && collected == 1 && bodies == 1 && replacements == 1 &&
                !reward.OverlapsBody(player), "full original shelf collects sixth without entering supply");
            phase = "lower-return"; await Walk(5825);
            for (int i = 0; i < 90 && !Supported(floor, floorShape); i++) await Tick();
            ReleaseAll(player.GlobalPosition); ulong releaseSequence = input.CurrentFrameSequence;
            bool Neutral() => input.CurrentFrameSequence > releaseSequence && input.CurrentFrame.MoveAxis == 0 &&
                input.CurrentFrame.Aim == Vector2.Zero && !input.CurrentFrame.JumpHeld && !input.CurrentFrame.JumpPressed &&
                !input.CurrentFrame.ClawPressed && !Input.IsPhysicalKeyPressed(Key.K) && player.Velocity.Length() <= 2;
            for (int i = 0; i < 90 && !(Neutral() && Idle()); i++) await Tick();
            Check(Neutral() && Idle() && idle && Supported(floor, floorShape) &&
                keyboard.ClawEdges == clawEdges + 1 && claw.CurrentFrame.NextCastId == nextCast + 1,
                "same player returns fresh neutral/Idle to full lower support, one cast and no added jump");
            returnedTick = Engine.GetPhysicsFrames(); Disk();
            GD.Print($"NATURAL_FRAGMENT_ROUTE_RETURNED count=6 player={playerId} physics={returnedTick} position={player.GlobalPosition}");
            phase = "same-gate-return"; await Walk(6120);
            for (int i = 0; i < 12 && barriers.Any(s => !s.Disabled); i++) await Tick();
            Check(barriers.All(s => s.Disabled) && BossAdmissionModel.Evaluate(level.Progress).Allowed &&
                Supported(arenaFloor, arenaFloorShape), "same gate now opens all three hull faces from sole sixth-ID transaction");
            openedTick = Engine.GetPhysicsFrames(); Disk();
            GD.Print($"NATURAL_FRAGMENT_ROUTE_GATE_OPEN count=6 player={playerId} physics={openedTick} position={player.GlobalPosition}");
            return new NaturalFragmentReceipt(playerId, bossId, blockedTick, collectedTick, returnedTick, openedTick,
                5, 6, bodies, collected, replacements, boosts, releases, diskHash, hp, health.CurrentHealth,
                nextCast, claw.CurrentFrame.NextCastId, jumps, dashes, requests, recoveries, applications,
                entryIds, exitIds, snapshotHash);
        }
        finally
        {
            // Evidence listeners only; production bindings are never disconnected.
            if (bodySubscribed && GodotObject.IsInstanceValid(fragment)) fragment.BodyEntered -= OnBody;
            if (collectionSubscribed && GodotObject.IsInstanceValid(fragment)) fragment.Collected -= OnCollected;
            if (clawSubscribed && GodotObject.IsInstanceValid(claw)) claw.ClawFrameChanged -= OnClaw;
            ReleaseAll(Live(player) ? player.GlobalPosition : Vector2.Zero);
            if (preInstalled) swapPre(previousPre);
            if (guardInstalled) swapGuard(previousGuard);
        }
    }
}
