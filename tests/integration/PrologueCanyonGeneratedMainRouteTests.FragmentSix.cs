using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Godot;
using Knight.Abilities;
using Knight.Boss;
using Knight.Combat;
using Knight.Player;
using Knight.Prologue;
using Knight.Prologue.Collections;
using Knight.Prologue.Seamless;
using Knight.Save;
using Knight.Shared;
using Knight.Tests.Presentation;

namespace Knight.Tests.Integration;

public static partial class PrologueCanyonGeneratedMainRouteTests
{
    private static string FragmentSixLatestAttackFact(CharacterBody2D player)
    {
        try
        {
            var receiver = player.GetNode<PlayerGuardedDamageReceiver>("GuardedDamage");
            var field = typeof(PlayerGuardedDamageReceiver).GetField("_processedAttackOrder", BindingFlags.NonPublic | BindingFlags.Instance);
            if (field?.GetValue(receiver) is not Queue<DamageAttackIdentity> identities || identities.Count == 0)
                return "receiver_identity=none";
            DamageAttackIdentity identity = identities.Last();
            Node source = GodotObject.InstanceFromId(identity.SourceRuntimeId) as Node;
            string path = source != null && GodotObject.IsInstanceValid(source) && source.IsInsideTree() ? source.GetPath().ToString() : "not-live";
            return $"receiver_identity={identity.SourceRuntimeId}/{identity.LocalAttackId} source={path}";
        }
        catch (Exception error) { return $"receiver_diagnostic_gap={error.GetType().Name}"; }
    }

    // A blocked/recovery-only return, a substituted collectible, an injected ID,
    // or a missing disk transaction must fail this SAME-player regional proof.
    // Continue prerequisites remain disclosed; this is not natural Title-to-door.
    private static async Task DriveFragmentSixFromCamp(SceneTree tree, PrologueLevelController level,
        PrologueGameFlowController flow, SeamlessPrologueWorld world, CharacterBody2D player,
        RouteMonitor monitor, KeyboardRouteInputDriver keyboard,
        Func<int> sessions, Func<int> requests, Func<int> recoveries)
    {
        ulong playerId = player.GetInstanceId();
        var health = player.GetNode<HealthComponent>("Health");
        string[] expected = { "abyss-floor", "ascent-mid", "courtyard", "gear-gallery", "steam-loop", "temple-upper" };
        expected = expected.Select(id => "canyon.v2.fragment." + id).ToArray();
        TestAssert.True(level.Progress.CollectedFragmentIds.OrderBy(id => id, StringComparer.Ordinal)
            .SequenceEqual(new[] { expected[0], expected[2] }), "six-return starts from the witnessed original two IDs");
        var seen = new HashSet<string>(level.Progress.CollectedFragmentIds, StringComparer.Ordinal);
        var points = new Dictionary<string, Vector2>(StringComparer.Ordinal) {
            [expected[1]] = new(4730, 3044), [expected[3]] = new(3160, 3720),
            [expected[4]] = new(3380, 4200), [expected[5]] = new(5896, 924) };
        var input = player.GetNode<PlayerInputReader>("Input");
        var motor = player.GetNode<PlayerMotor>("Motor");
        var claw = player.GetNode<MechanicalClawController>("MechanicalClaw");
        var body = player.GetNode<CollisionShape2D>("BodyCollider");
        var evidence = new Task10RouteEvidence(level, world, player);
        int overlaps = 0, previousHp = health.CurrentHealth;
        void Check(bool value, string message) => TestAssert.True(value,
            $"six-return {message}; player={playerId}/{player.GetInstanceId()} pos={player.GlobalPosition} " +
            $"velocity={player.Velocity} hp={health.CurrentHealth} count={level.Progress.CollectedFragmentIds.Count} " +
            $"recovery={requests()}/{recoveries()} claw={claw.CurrentFrame.State}/{claw.CurrentFrame.Failure}");
        void Observe()
        {
            monitor.Observe(); evidence.Observe();
            Check(level.Player == player && monitor.Player == player && player.GetInstanceId() == playerId &&
                flow.State == PrologueGameFlowState.Playing && !tree.Paused && health.CurrentHealth > 0 &&
                health.CurrentHealth <= previousHp && sessions() == 1 && requests() == 0 && recoveries() == 0 &&
                monitor.RecoveryRequested == 0 && monitor.RecoveryCompleted == 0 &&
                !level.Progress.BossDefeated && !flow.Save.LegacyCollectionQuotaExempt &&
                flow.Save.CheckpointId == "canyon.v2.temple-forecourt" &&
                player.GlobalPosition.IsFinite() && player.Velocity.IsFinite() &&
                body.Shape is CapsuleShape2D { Radius: 20, Height: 72 } && !body.Disabled,
                "keeps one alive same player, no healing/recovery/session/physics or checkpoint substitution");
            previousHp = health.CurrentHealth;
            string[] current = level.Progress.CollectedFragmentIds.OrderBy(id => id, StringComparer.Ordinal).ToArray();
            Check(seen.All(current.Contains) && current.All(expected.Contains), "only the literal six IDs grow monotonically");
            foreach (string id in current.Where(id => !seen.Contains(id)))
            {
                QuestFragment fragment = world.FindChildren("*", "", true, false).OfType<QuestFragment>()
                    .Single(value => value.StableId == id);
                var shape = fragment.GetNode<CollisionShape2D>("Collision");
                Vector2 center = body.GlobalPosition;
                Vector2 nearest = new(center.X, Mathf.Clamp(shape.GlobalPosition.Y, center.Y - 16, center.Y + 16));
                Check(points.ContainsKey(id) && fragment.GlobalPosition == points[id] &&
                    shape.Shape is CircleShape2D { Radius: 18 } && !shape.Disabled && fragment.Monitoring &&
                    fragment.IsCollected && fragment.FeedbackCount == 1 && fragment.LastResult == FragmentCollectionResult.Collected &&
                    fragment.GetOverlappingBodies().Contains(player) && nearest.DistanceTo(shape.GlobalPosition) <= 38.1f,
                    "new ID is the original native collectible and actual capsule/area contact, not a save injection");
                PrologueSaveData disk = Task18RawSave(File.ReadAllBytes(flow.SavePath));
                Check(disk.Version == 4 && disk.LayoutVersion == 2 && disk.CollectionRuleVersion == 1 &&
                    !disk.LegacyCollectionQuotaExempt && disk.CollectedFragmentIds.OrderBy(value => value, StringComparer.Ordinal).SequenceEqual(current) &&
                    flow.Save.CollectedFragmentIds.OrderBy(value => value, StringComparer.Ordinal).SequenceEqual(current),
                    "each contact has already committed exact current IDs to isolated disk and live save");
                seen.Add(id); overlaps++;
                GD.Print($"QUEST_FRAGMENT_SIX_CONTACT id={id} player={playerId} count={current.Length} physics={Engine.GetPhysicsFrames()} position={player.GlobalPosition}");
            }
        }
        Observe();
        await DriveTask10NormalRoute(tree, level, world, player, monitor, evidence, Observe,
            Task10RouteVariant.Exploration, false, null, fragmentReturnFromCamp: true);
        Observe();
        Check(seen.Count == 5 && overlaps == 3, "four-region continuation returns five genuine IDs to the original Temple floor");
        Check(world.TryGetMountedSector(PrologueSectorId.LostTemple, out PrologueSector temple), "actual Temple is mounted again");
        var floor = temple.GetNode<StaticBody2D>("Gameplay/Collision/CheckpointFloor");
        var shelf = temple.GetNode<StaticBody2D>("Gameplay/Collision/TempleSupplyShelf");
        var beam = temple.GetNode<GrappleSurface2D>("Gameplay/Collision/TempleSupplyGripBeam/GrappleSurfaceProfile");
        var arena = temple.GetNode<BossArenaController>("Gameplay/BossArenaController");
        int hp = health.CurrentHealth, jumps = motor.AcceptedJumpCount, dashes = motor.AcceptedDashCount;
        ulong cast = claw.CurrentFrame.NextCastId;
        int boosts = 0, releases = 0;
        void OnClaw(MechanicalClawFrame frame, GrappleSurface2D target)
        {
            if (frame.State == MechanicalClawState.Boost && frame.CastId == cast && frame.BoostConsumed &&
                frame.ReleaseKind == MechanicalClawReleaseKind.Boost && target == beam) boosts++;
            if (frame.State == MechanicalClawState.Released && frame.CastId == cast && frame.BoostConsumed &&
                frame.ReleaseKind == MechanicalClawReleaseKind.Boost) releases++;
        }
        bool Supported(StaticBody2D support)
        {
            var shape = support.GetNode<CollisionShape2D>("Collision");
            return TryTask10CapsuleCorridor(player, shape, out _, out _, out _, out _) && HasFloorContact(player, support);
        }
        async Task Tick()
        {
            await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
            Observe();
            Check(health.CurrentHealth == hp && motor.AcceptedJumpCount == jumps && motor.AcceptedDashCount == dashes &&
                !arena.IsEncounterActive, "original beam Boost/return needs no ordinary jump, healing, dash or premature Boss activation");
        }
        PrologueCanyonTraversalTests.BeginTask10InputDrive(tree, player, Observe);
        claw.ClawFrameChanged += OnClaw;
        try
        {
            Check(Supported(floor), "returns with full original checkpoint-floor support");
            await PrologueCanyonTraversalTests.Task4WalkTo(tree, player, 5825f, 120);
            Check(Supported(floor), "walks back to the real beam stance");
            SendAction(InputActions.MoveUp, true);
            for (int i = 0; i < 3; i++) await Tick();
            MechanicalClawTargetResolution preview = new MechanicalClawTargetResolver().ResolveKeyboard(player, Vector2.Up);
            Check(preview.Surface == beam && preview.Snapshot.HasLineOfSight && !preview.Snapshot.IsLegacyInteraction &&
                player.GlobalPosition.DistanceTo(preview.Snapshot.WorldAnchor) <= 620, "actual physical W selects the original visible beam");
            SendAction(InputActions.Claw, true);
            for (int i = 0; i < 3; i++) await Tick();
            SendAction(InputActions.Claw, false); SendAction(InputActions.MoveUp, false);
            for (int i = 0; i < 300 && !claw.CurrentFrame.Arrived; i++) await Tick();
            Check(claw.CurrentTarget == beam && claw.CurrentFrame.Arrived && claw.CurrentFrame.State == MechanicalClawState.Swing &&
                claw.CurrentFrame.NextCastId == cast + 1, "one real cast naturally arrives before Space");
            SendAction(InputActions.MoveRight, true); SendAction(InputActions.Jump, true);
            for (int i = 0; i < 3; i++) await Tick();
            SendAction(InputActions.Jump, false);
            await PrologueCanyonTraversalTests.Task4WalkTo(tree, player, 5896f, 120);
            for (int i = 0; i < 90 && !Supported(shelf); i++) await Tick();
            Check(Supported(shelf) && boosts == 1 && releases == 1 && seen.Count == 6 && overlaps == 4,
                "one physical Space Boost lands on full shelf and obtains the genuine sixth contact");
            await PrologueCanyonTraversalTests.Task4WalkTo(tree, player, 5825f, 120);
            for (int i = 0; i < 90 && !Supported(floor); i++) await Tick();
            Check(Supported(floor), "returns from the sixth pickup to the original lower floor");
            await PrologueCanyonTraversalTests.Task4WalkTo(tree, player, 5920f, 120);
            keyboard.ReleaseAll();
            for (int i = 0; i < 6; i++) await Tick();
            PrologueSaveData disk = Task18RawSave(File.ReadAllBytes(flow.SavePath));
            Check(seen.OrderBy(id => id, StringComparer.Ordinal).SequenceEqual(expected) &&
                disk.CollectedFragmentIds.OrderBy(id => id, StringComparer.Ordinal).SequenceEqual(expected) &&
                Supported(floor) && player.IsOnFloor() && player.Velocity.Length() <= 2 &&
                claw.CurrentFrame.State == MechanicalClawState.Idle && BossAdmissionModel.Evaluate(level.Progress).Allowed,
                "six-return same-player must actually obtain six different original fragments before returning to Temple");
            string hash = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(flow.SavePath)));
            GD.Print($"QUEST_FRAGMENT_SIX_OK player={playerId} checkpoint={flow.Save.CheckpointId} fragments_before=0 fragments_after=6 new_overlaps={overlaps} ids=abyss-floor,ascent-mid,courtyard,gear-gallery,steam-loop,temple-upper supported=true noRecovery=true seededIds=false fixture=true fullRoute=false save_sha={hash}");
        }
        finally
        {
            claw.ClawFrameChanged -= OnClaw;
            keyboard.ReleaseAll();
            PrologueCanyonTraversalTests.EndTask10InputDrive();
        }
    }
}
