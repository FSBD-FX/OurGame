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
    private sealed record Task18FragmentReceipt(string StableId, ulong NodeId, ulong ShapeId,
        Vector2 WorldPosition, float Radius, string EntryIds, string ExitIds,
        int EntryCount, int ExitCount, int EntryFeedback, int ExitFeedback,
        int CollectionBodyEntries, int ReturnBodyEntries, int CollectedEvents, int SaveReplacements,
        ulong CollectionBodyPlayerId, ulong ReturnBodyPlayerId, string CollectionDiskHash);

    // Value-only handoff; only the independently witnessed fragment transaction may
    // replace Save, and only the original supply may change HP/discoveries afterward.
    private sealed record Task18NormalSupplyReceipt(ulong PlayerId, ulong BossId,
        int EntryHp, int ExitHp, int MaxHp, ulong EntryNextCast, ulong ExitNextCast,
        int Requests, int Recoveries, int Jumps, int Dashes, int ChainApplications,
        int HealedEvents, int BoostEvents, int ReleaseEvents, string SnapshotHash, Task18FragmentReceipt Fragment);

    private static string Task18Ids(IEnumerable<string> ids) =>
        string.Join("|", ids.OrderBy(id => id, StringComparer.Ordinal));

    // Read the retained wire values without production Normalize/ToSave. All twenty
    // persisted fields must exist exactly once; set ordering is not a gameplay value.
    private static PrologueSaveData Task18RawSave(byte[] bytes)
    {
        using JsonDocument wire = JsonDocument.Parse(bytes);
        string[] names = { "version", "layoutVersion", "checkpointId", "openShortcuts", "activatedMechanisms",
            "collectionRuleVersion", "legacyCollectionQuotaExempt", "collectedFragmentIds", "hasActivatedCheckpoint",
            "checkpointSector", "checkpointWorldX", "checkpointWorldY", "shortcutOpen", "discoveries",
            "bossDefeated", "completed", "objective", "completedTutorials", "bestHumanCompletionSeconds", "bestHumanDeaths" };
        var properties = wire.RootElement.EnumerateObject().ToArray();
        var optional = properties.Where(p => p.Name == "explorationMap").ToArray();
        TestAssert.True(optional.Length <= 1 && optional.All(p => p.Value.ValueKind is JsonValueKind.Object or JsonValueKind.Null),
            "Task18 optional explorationMap exists at most once with object/null kind");
        TestAssert.True(properties.Where(p => p.Name != "explorationMap").Select(p => p.Name).OrderBy(s => s, StringComparer.Ordinal)
            .SequenceEqual(names.OrderBy(s => s, StringComparer.Ordinal)), "Task18 exact twenty-field raw save schema");
        return JsonSerializer.Deserialize<PrologueSaveData>(bytes,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }) ??
            throw new InvalidDataException("Task18 raw save has no value");
    }

    private static bool Task18SaveExact(PrologueSaveData actual, PrologueSaveData baseline,
        string fragmentIds, string discoveries) => actual != null && !actual.WasRecovered &&
        actual.Version == baseline.Version && actual.LayoutVersion == baseline.LayoutVersion &&
        actual.CheckpointId == baseline.CheckpointId &&
        Task18Ids(actual.OpenShortcuts) == Task18Ids(baseline.OpenShortcuts) &&
        Task18Ids(actual.ActivatedMechanisms) == Task18Ids(baseline.ActivatedMechanisms) &&
        actual.CollectionRuleVersion == baseline.CollectionRuleVersion &&
        actual.LegacyCollectionQuotaExempt == baseline.LegacyCollectionQuotaExempt &&
        Task18Ids(actual.CollectedFragmentIds) == fragmentIds &&
        actual.HasActivatedCheckpoint == baseline.HasActivatedCheckpoint && actual.CheckpointSector == baseline.CheckpointSector &&
        actual.CheckpointWorldX == baseline.CheckpointWorldX && actual.CheckpointWorldY == baseline.CheckpointWorldY &&
        actual.ShortcutOpen == baseline.ShortcutOpen && Task18Ids(actual.Discoveries) == discoveries &&
        actual.BossDefeated == baseline.BossDefeated && actual.Completed == baseline.Completed &&
        actual.Objective == baseline.Objective && Task18Ids(actual.CompletedTutorials) == Task18Ids(baseline.CompletedTutorials) &&
        actual.BestHumanCompletionSeconds == baseline.BestHumanCompletionSeconds && actual.BestHumanDeaths == baseline.BestHumanDeaths;

    // Catch broken terrain resolution/Space suppression, unsupported plate opening,
    // reward ordering/HUD/persistence, or identity/progression loss on an ordinary route.
    // No fixture assembly, reseeding, gameplay writes, or replacement input-drive owner.
    private static async Task<Task18NormalSupplyReceipt> DriveTask18NormalTempleSupply(
        SceneTree tree, PrologueLevelController level, SeamlessPrologueWorld world,
        CharacterBody2D player, RouteMonitor monitor, Action observe,
        Func<Action, Action> swapApproachGuard, Func<Action, Action> swapPreObserverGuard,
        string snapshotPath)
    {
        const string rewardId = "canyon.v2.reward.temple-supply";
        const string fragmentId = "canyon.v2.fragment.temple-upper";
        const string checkpointId = "canyon.v2.temple-forecourt";
        bool Live(Node node) => node != null && GodotObject.IsInstanceValid(node) &&
            node.IsInsideTree() && !node.IsQueuedForDeletion();
        void Check(bool value, string message) => TestAssert.True(value, "Task18 normal supply: " + message);
        Check(GodotObject.IsInstanceValid(tree) && new Node[] { level, world, player, monitor.Player }.All(Live),
            "requires original live route nodes before component reads");
        Check(world.TryGetMountedSector(PrologueSectorId.LostTemple, out PrologueSector temple) && Live(temple),
            "requires original mounted Temple");
        var health = player.GetNode<HealthComponent>("Health");
        var input = player.GetNode<PlayerInputReader>("Input");
        var keyboardDriver = KeyboardRouteInputDriver.Current;
        bool keyboardRoute = keyboardDriver != null;
        int entryKeyboardAttacks = keyboardDriver?.AttackEdges ?? 0;
        int entryKeyboardClaws = keyboardDriver?.ClawEdges ?? 0;
        var motor = player.GetNode<PlayerMotor>("Motor");
        var modes = player.GetNode<PlayerActionModeController>("ActionModes");
        var claw = player.GetNode<MechanicalClawController>("MechanicalClaw");
        var bodyShape = player.GetNode<CollisionShape2D>("BodyCollider");
        var floor = temple.GetNode<StaticBody2D>("Gameplay/Collision/CheckpointFloor");
        var shelf = temple.GetNode<StaticBody2D>("Gameplay/Collision/TempleSupplyShelf");
        var beam = temple.GetNode<GrappleSurface2D>("Gameplay/Collision/TempleSupplyGripBeam/GrappleSurfaceProfile");
        var gate = temple.GetNode<CanyonPressurePlateGate>("Gameplay/Mechanisms/TemplePressureGate");
        var reward = temple.GetNode<PrologueSupplyCache>("Gameplay/Rewards/TempleSupply");
        var chain = temple.GetNode<BellChainMechanism>("Gameplay/Mechanisms/BellChain");
        var arena = temple.GetNode<BossArenaController>("Gameplay/BossArenaController");
        var flow = level.GetNode<PrologueGameFlowController>("GameFlow");
        var hud = level.GetNode<PrologueFormalHud>("Hud/FormalHud");
        var fragment = temple.GetNode<QuestFragment>("Gameplay/QuestFragments/temple-upper");
        Check(new Node[] { health, input, motor, modes, claw, bodyShape, floor, shelf,
            beam, gate, reward, chain, arena, flow, hud, fragment }.All(Live), "requires native-live detour components");
        var fragmentShape = fragment.GetNode<CollisionShape2D>("Collision");
        var fragmentVisual = fragment.GetNode<Node2D>("AvailableVisual");
        var floorShape = floor.GetNode<CollisionShape2D>("Collision");
        var shelfShape = shelf.GetNode<CollisionShape2D>("Collision");
        var chainShape = chain.GetNode<CollisionShape2D>("Door/Collision");
        var adapter = chain.GetNode<LegacyThreadInteractionAdapter>("ThreadTarget/LegacyThreadInteractionAdapter");
        FallenBellkeeper boss = arena.CurrentBoss;
        Check(Live(boss), "requires actual original Boss before reading health");
        var bossHealth = boss.GetNode<HealthComponent>("Health");
        var bossTrigger = arena.GetNode<Area2D>(arena.ActivationTriggerPath);
        var gateDoor = gate.GetNode<StaticBody2D>("Door");
        var gateShape = gate.DoorCollision;
        Node[] nodes = { level, world, temple, player, monitor.Player, health, input, motor, modes,
            claw, bodyShape, floor, shelf, beam, gate, reward, chain, arena, flow, hud,
            floorShape, shelfShape, chainShape, adapter, boss, bossHealth, bossTrigger, gateDoor, gateShape,
            fragment, fragmentShape, fragmentVisual };
        Check(nodes.All(Live), "requires live supports, chain adapter and original Boss before baselines");
        ulong[] nodeIds = nodes.Select(n => n.GetInstanceId()).ToArray();
        CollisionShape2D[] shapes = { bodyShape, floorShape, shelfShape, chainShape, gateShape, fragmentShape };
        Check(fragmentShape.Shape is CircleShape2D && GodotObject.IsInstanceValid(fragmentShape.Shape),
            "requires original native circular fragment area before resource reads");
        var fragmentCircle = (CircleShape2D)fragmentShape.Shape;
        ulong fragmentNodeId = fragment.GetInstanceId(), fragmentShapeId = fragmentCircle.GetInstanceId();
        Transform2D fragmentTransform = fragmentShape.GlobalTransform;
        void Native()
        {
            Check(GodotObject.IsInstanceValid(tree) && nodes.All(Live), "keeps native nodes live before reads/callbacks");
            Check(nodes.Select(n => n.GetInstanceId()).SequenceEqual(nodeIds) &&
                shapes.All(s => GodotObject.IsInstanceValid(s.Shape)), "keeps original identities and collision resources");
            Check(fragment.StableId == fragmentId && fragment.GlobalPosition == new Vector2(5896, 924) &&
                fragmentShape.Shape == fragmentCircle && fragmentCircle.GetInstanceId() == fragmentShapeId &&
                fragmentCircle.Radius == 18 && fragmentShape.GlobalTransform == fragmentTransform &&
                fragmentTransform.Origin == fragment.GlobalPosition && fragmentTransform.X == Vector2.Right &&
                fragmentTransform.Y == Vector2.Down && !fragmentShape.Disabled && fragment.Monitoring &&
                !fragment.Monitorable && fragment.CollisionLayer == 0 && fragment.CollisionMask == 2,
                "keeps exact original temple-upper node, world position and native radius18 area");
        }
        Native();
        ulong playerId = player.GetInstanceId(), bossId = boss.GetInstanceId(), beamId = beam.GetInstanceId();
        int entryHp = health.CurrentHealth, maxHp = health.MaxHealth, expectedHp = Math.Min(maxHp, entryHp + 2);
        int jumps = motor.AcceptedJumpCount, dashes = motor.AcceptedDashCount;
        int requests = monitor.RecoveryRequested, recoveries = monitor.RecoveryCompleted;
        int applications = adapter.ApplicationCount, bossHp = bossHealth.CurrentHealth;
        ulong nextCast = claw.CurrentFrame.NextCastId;
        string savePath = flow.SavePath;
        string storageArgument = OS.GetCmdlineUserArgs().SingleOrDefault(s => s.StartsWith("--case-storage-root=", StringComparison.Ordinal));
        Check(storageArgument != null && Path.IsPathFullyQualified(savePath) && Path.IsPathFullyQualified(snapshotPath) &&
            Task10ApplicationPersistenceGuard.IsWithin(savePath, storageArgument["--case-storage-root=".Length..]) &&
            Task10ApplicationPersistenceGuard.IsWithin(snapshotPath, storageArgument["--case-storage-root=".Length..]),
            "uses only actual isolated case-local save and retained snapshot paths");
        PrologueSaveData initialSave = flow.Save, acceptedSave = initialSave;
        string[] shortcuts = level.Progress.OpenShortcuts.OrderBy(s => s, StringComparer.Ordinal).ToArray();
        string[] mechanisms = level.Progress.ActivatedMechanisms.OrderBy(s => s, StringComparer.Ordinal).ToArray();
        string[] discoveries = level.Progress.Discoveries.OrderBy(s => s, StringComparer.Ordinal).ToArray();
        StringName[] tutorials = level.Progress.CompletedTutorials.OrderBy(s => s.ToString(), StringComparer.Ordinal).ToArray();
        var encounterNodes = world.FindChildren("*", "", true, false);
        using var encounterNodesOwner = (Godot.Collections.Array)encounterNodes;
        var encounters = encounterNodes.OfType<CanyonEncounterGroup>().ToArray();
        Check(encounters.All(Live), "requires native-valid actual encounter baselines");
        var encounterFacts = encounters.Select(e => (e.GetInstanceId(), e.IsActive, e.IsComplete, e.LiveActorCount)).ToArray();
        var actorBodies = encounterNodes.OfType<CharacterBody2D>().ToArray();
        Check(actorBodies.All(Live), "requires native-valid upstream actors before component reads");
        var enemies = actorBodies.Where(e => (e.CollisionLayer & CombatCollisionLayers.Enemy) != 0).ToArray();
        ulong[] enemyIds = enemies.Select(e => e.GetInstanceId()).ToArray();
        int retiredSectors = world.RetiredSectorCount;
        byte[] snapshotBytes = File.ReadAllBytes(snapshotPath);
        PrologueSaveData saveBaseline = Task18RawSave(snapshotBytes);
        string[] entryFragments = saveBaseline.CollectedFragmentIds.ToArray();
        string entryFragmentIds = Task18Ids(entryFragments);
        string exitFragmentIds = Task18Ids(entryFragments.Append(fragmentId));
        int entryFragmentFeedback = fragment.FeedbackCount;
        Check(QuestFragmentCatalog.Definitions.Single(d => d.StableId == fragmentId).Optional &&
            entryFragments.All(QuestFragmentCatalog.IsKnown) && entryFragments.Distinct(StringComparer.Ordinal).Count() == entryFragments.Length &&
            !entryFragments.Contains(fragmentId) && !fragment.IsCollected && fragmentVisual.Visible &&
            !fragment.GetOverlappingBodies().Contains(player) && !saveBaseline.LegacyCollectionQuotaExempt &&
            !level.Progress.LegacyCollectionQuotaExempt && Task18Ids(level.Progress.CollectedFragmentIds) == entryFragmentIds &&
            hud.CurrentModel.FragmentCount == entryFragments.Length && hud.CurrentModel.FragmentRequired == 6 &&
            !hud.CurrentModel.CollectionQuotaExempt &&
            Task18SaveExact(initialSave, saveBaseline, entryFragmentIds, Task18Ids(discoveries)),
            "starts from independent retained values with the exact optional fragment genuinely uncollected");
        string snapshotHash = Convert.ToHexString(SHA256.HashData(snapshotBytes));
        Check(snapshotBytes.SequenceEqual(File.ReadAllBytes(savePath)), "starts from just-retained byte-identical pre-Boss disk save");
        Check(entryHp > 0 && entryHp < maxHp, $"requires genuinely injured arrival, measured HP={entryHp}/{maxHp}; no synthetic injury");
        Check(!reward.IsSpent && !discoveries.Contains(rewardId) && reward.DiscoveryId == rewardId && reward.HealthAmount == 2 &&
            gate.MechanismId == "canyon.v2.pressure-gate.temple-reward" && gate.PlayerBody == player,
            "requires original unspent +2 supply and bound actual pressure gate");

        string phase = "entry";
        var shelfDiagnosticRows = new List<string>(256);
        int shelfDiagnosticOrdinal = 0, shelfDiagnosticOmitted = 0, shelfDiagnosticFaults = 0;
        string shelfDiagnosticFirstFault = "none";
        string shelfConsumerControl = "original-one-sided";
        void RecordShelf(Func<string> payload)
        {
            if (!keyboardRoute || phase != "shelf-landing") return;
            int ordinal = ++shelfDiagnosticOrdinal;
            if (shelfDiagnosticRows.Count >= 256) { shelfDiagnosticOmitted++; return; }
            try
            {
                shelfDiagnosticRows.Add($"TASK18_SHELF_DIAGNOSTIC ordinal={ordinal} physics={Engine.GetPhysicsFrames()} " +
                    $"sample={input.CurrentFrameSequence} {payload()}");
            }
            catch (Exception error)
            {
                shelfDiagnosticFaults++;
                if (shelfDiagnosticFirstFault == "none") shelfDiagnosticFirstFault = error.GetType().Name;
            }
        }
        void RecordShelfMotion(string boundary, float target, int originalRemaining)
        {
            RecordShelf(() =>
            {
                Rect2 body = Bounds();
                bool floorNow = player.IsOnFloor();
                MobilityModuleSettings module = motor.ThrusterState.Module;
                var stop = FreeMotionForecast.Neutral(player.Velocity.X, floorNow, motor.MotionSettings,
                    module, 1d / Engine.PhysicsTicksPerSecond, originalRemaining, 2f);
                return $"boundary={boundary} timing=callsite-or-original-post-process-Tick target_x={target:R} " +
                    $"position={player.GlobalPosition} velocity={player.Velocity} body={body} on_floor={floorNow} " +
                    $"move={input.CurrentFrame.MoveAxis} raw_AD={Input.IsPhysicalKeyPressed(Key.A)}/{Input.IsPhysicalKeyPressed(Key.D)} " +
                    $"module={module.ModuleId} friction={module.GroundFriction:R} drag={module.AirDragRate:R} " +
                    $"remaining={originalRemaining} neutral_stop_x={player.GlobalPosition.X + stop.NetDistance:R} " +
                    $"forecast_steps={stop.Steps} threshold_reached={stop.SpeedThresholdReached} " +
                    $"named_support=not_queried_for_motion_snapshot consumer_control={shelfConsumerControl} recorder_only=true";
            });
        }
        Exception callbackFailure = null, firstFailure = null;
        var cleanupFailures = new List<Exception>();
        Action previousGuard = null, previousPre = null;
        bool guardInstalled = false, preInstalled = false;
        bool clawSubscribed = false, healthSubscribed = false, rewardSubscribed = false, persistSubscribed = false;
        bool fragmentBodySubscribed = false, fragmentCollectedSubscribed = false;
        int collectionBodyEntries = 0, returnBodyEntries = 0, fragmentCollectedEvents = 0, saveReplacements = 0;
        ulong collectionBodyPlayerId = 0, returnBodyPlayerId = 0;
        string collectionDiskHash = string.Empty, returnDiskHash = string.Empty;
        bool castIssued = false, spaceIssued = false, travel = false, latched = false, arrived = false, idle = false;
        bool keyboardAimHeld = false, keyboardCastHeld = false;
        int heals = 0, overlaps = 0, persists = 0, boosts = 0, releases = 0, castSamples = 0, spaceSamples = 0;
        int publication = 0, boostPublication = 0, waits = 0;
        ulong arrivalTick = 0, spaceTick = 0, boostTick = 0, castSequence = 0, spaceSequence = 0;
        ulong keyboardReadySequence = 0, castIssueSequence = 0;
        Vector2 aim = new(5828f, 840f);
        var resolver = new MechanicalClawTargetResolver();

        bool Idle() => claw.CurrentFrame.State == MechanicalClawState.Idle && claw.CurrentFrame.CastId == 0 &&
            claw.CurrentFrame.LockedTargetRuntimeId == 0 && claw.CurrentFrame.Failure == MechanicalClawFailure.None &&
            claw.CurrentTarget == null && claw.ResidencyTarget == null;
        Rect2 Bounds()
        {
            Check(bodyShape.Shape is CapsuleShape2D, "uses actual capsule geometry");
            var capsule = (CapsuleShape2D)bodyShape.Shape;
            Transform2D t = bodyShape.GlobalTransform;
            Check(t.Origin.IsFinite() && t.X.IsFinite() && t.Y.IsFinite() &&
                Mathf.Abs(t.X.Y) <= .001f && Mathf.Abs(t.Y.X) <= .001f &&
                float.IsFinite(capsule.Radius) && capsule.Radius > 0 && float.IsFinite(capsule.Height) &&
                capsule.Height >= 2 * capsule.Radius, "requires finite upright full capsule");
            Vector2 half = new(capsule.Radius * Mathf.Abs(t.X.X), capsule.Height * .5f * Mathf.Abs(t.Y.Y));
            Check(half.IsFinite() && half.X > 0 && half.Y > 0 && !bodyShape.Disabled, "uses enabled full body");
            return new Rect2(t.Origin - half, half * 2);
        }
        bool Supported(StaticBody2D support, CollisionShape2D shape, float top, string diagnosticUse = null)
        {
            if (shape.Disabled || shape.Shape is not RectangleShape2D rect || !player.IsOnFloor())
            {
                if (diagnosticUse != null) RecordShelf(() => $"use={diagnosticUse} support={support.Name} " +
                    "supported=false early=shape-or-floor rays_executed=0 remaining_rays=not-evaluated-short-circuit");
                return false;
            }
            Rect2 b = Bounds();
            float left = shape.ToGlobal(new Vector2(-rect.Size.X / 2, 0)).X;
            float right = shape.ToGlobal(new Vector2(rect.Size.X / 2, 0)).X;
            if (b.Position.X < left || b.End.X > right || Mathf.Abs(b.End.Y - top) > 1 || Mathf.Abs(player.Velocity.Y) > .01f)
            {
                if (diagnosticUse != null) RecordShelf(() => $"use={diagnosticUse} support={support.Name} body={b} " +
                    $"support_x={left:R}/{right:R} top={top:R} vy={player.Velocity.Y:R} supported=false " +
                    "early=body-geometry rays_executed=0 remaining_rays=not-evaluated-short-circuit");
                return false;
            }
            // Same downward center/edge foot queries as ClosedChain physical support.
            // Stationary MoveAndSlide may have no fresh slide contact; never accept IsOnFloor alone.
            int rayIndex = 0;
            foreach (float x in new[] { b.GetCenter().X, b.Position.X + 1, b.End.X - 1 })
            {
                using var query = PhysicsRayQueryParameters2D.Create(new Vector2(x, b.End.Y - .25f),
                    new Vector2(x, b.End.Y + 1), CombatCollisionLayers.World);
                query.CollideWithBodies = true; query.CollideWithAreas = false;
                using var hit = player.GetWorld2D().DirectSpaceState.IntersectRay(query);
                if (hit.Count == 0 || !hit.ContainsKey("collider"))
                {
                    if (diagnosticUse != null) RecordShelf(() => $"use={diagnosticUse} support={support.Name} ray={rayIndex} " +
                        $"x={x:R} hit=empty-or-no-collider supported=false rays_executed={rayIndex + 1} " +
                        "remaining_rays=not-evaluated-short-circuit");
                    return false;
                }
                GodotObject collider = hit["collider"].AsGodotObject();
                bool failed = !GodotObject.IsInstanceValid(collider) || collider != support ||
                    hit["normal"].AsVector2().Y > -.99f || Mathf.Abs(hit["position"].AsVector2().Y - b.End.Y) > 1;
                if (diagnosticUse != null) RecordShelf(() => $"use={diagnosticUse} support={support.Name} ray={rayIndex} " +
                    $"x={x:R} collider={(GodotObject.IsInstanceValid(collider) ? collider.GetInstanceId() : 0UL)} " +
                    $"same_support={collider == support} normal={hit["normal"].AsVector2()} point={hit["position"].AsVector2()} " +
                    $"passed={!failed} rays_executed={rayIndex + 1} remaining_rays={(failed ? "not-evaluated-short-circuit" : "continue-original-query-path")}");
                if (failed) return false;
                rayIndex++;
            }
            return true;
        }
        bool DoorClear(string diagnosticUse = null)
        {
            using var query = PhysicsRayQueryParameters2D.Create(new Vector2(5928, 924), new Vector2(5962, 924),
                CombatCollisionLayers.World);
            query.CollideWithBodies = true; query.CollideWithAreas = false;
            using var hit = player.GetWorld2D().DirectSpaceState.IntersectRay(query);
            if (hit.Count == 0)
            {
                if (diagnosticUse != null) RecordShelf(() => $"use={diagnosticUse} door_clear=true original_door_query=empty");
                return true;
            }
            if (diagnosticUse != null) RecordShelf(() => $"use={diagnosticUse} door_clear=false original_door_query=hit " +
                $"has_collider={hit.ContainsKey("collider")}");
            Check(hit.ContainsKey("collider") && GodotObject.IsInstanceValid(hit["collider"].AsGodotObject()) &&
                hit["collider"].AsGodotObject() == gateDoor, "real fixed door query cannot be replaced by unrelated terrain");
            return false;
        }
        void Progress()
        {
            var checkpoint = level.Progress.RestoreCheckpoint();
            Check(chain.IsOpen && chainShape.Disabled && adapter.ApplicationCount == applications &&
                level.Progress.BellChainShortcutOpen && checkpoint.HasActivatedCheckpoint &&
                checkpoint.CheckpointId == checkpointId && checkpoint.SectorId == PrologueSectorId.LostTemple &&
                checkpoint.WorldPosition == new Vector2(5920, 1080) && world.CheckpointId == checkpointId &&
                world.CurrentSector == PrologueSectorId.LostTemple && level.CurrentSectorRoot == temple &&
                !level.Progress.BossDefeated && !arena.IsEncounterActive && arena.CurrentBoss == boss &&
                !bossTrigger.GetOverlappingBodies().Contains(player) && bossHealth.CurrentHealth == bossHp && bossHp == bossHealth.MaxHealth,
                "preserves actual chain/checkpoint and inactive untouched original Boss");
            Check(shortcuts.SequenceEqual(level.Progress.OpenShortcuts.OrderBy(s => s, StringComparer.Ordinal)) &&
                mechanisms.SequenceEqual(level.Progress.ActivatedMechanisms.OrderBy(s => s, StringComparer.Ordinal)) &&
                tutorials.SequenceEqual(level.Progress.CompletedTutorials.OrderBy(s => s.ToString(), StringComparer.Ordinal)) &&
                discoveries.SequenceEqual(level.Progress.Discoveries.Where(s => s != rewardId).OrderBy(s => s, StringComparer.Ordinal)) &&
                level.Progress.Discoveries.Contains(rewardId) == (heals == 1) && reward.IsSpent == (heals == 1),
                "retains all upstream progress with only the finite supply discovery delta");
            Check(encounters.All(Live) && encounterFacts.SequenceEqual(encounters.Select(e =>
                (e.GetInstanceId(), e.IsActive, e.IsComplete, e.LiveActorCount))) &&
                enemies.All(Live) && enemyIds.SequenceEqual(enemies.Select(e => e.GetInstanceId())) &&
                world.RetiredSectorCount == retiredSectors,
                "retains measured upstream encounter outcomes/actors and sector retirement baseline");
        }
        void Safety()
        {
            Native();
            if (callbackFailure != null) throw new InvalidOperationException("Normal supply publication observation failed", callbackFailure);
            Check(level.Player == player && monitor.Player == player && flow.State == PrologueGameFlowState.Playing &&
                ReferenceEquals(acceptedSave, flow.Save) && flow.SavePath == savePath && !tree.Paused &&
                health.MaxHealth == maxHp && health.CurrentHealth == (heals == 0 ? entryHp : expectedHp) &&
                monitor.RecoveryRequested == requests && monitor.RecoveryCompleted == recoveries &&
                motor.AcceptedJumpCount == jumps && motor.AcceptedDashCount == dashes &&
                player.GlobalPosition.IsFinite() && player.Velocity.IsFinite(), "keeps same living player, HP and ordinary action/recovery baselines");
            string expectedFragments = fragmentCollectedEvents == 0 ? entryFragmentIds : exitFragmentIds;
            string expectedDiscoveries = Task18Ids(heals == 0 ? discoveries : discoveries.Append(rewardId));
            Check(fragmentCollectedEvents is 0 or 1 && saveReplacements == fragmentCollectedEvents &&
                collectionBodyEntries is 0 or 1 && returnBodyEntries is 0 or 1 &&
                (returnBodyEntries == 0 || collectionBodyEntries == 1 && fragmentCollectedEvents == 1) &&
                Task18SaveExact(flow.Save, saveBaseline, expectedFragments, expectedDiscoveries) &&
                Task18SaveExact(initialSave, saveBaseline, entryFragmentIds, Task18Ids(discoveries)) &&
                Task18Ids(level.Progress.CollectedFragmentIds) == expectedFragments &&
                level.Progress.CollectionRuleVersion == saveBaseline.CollectionRuleVersion &&
                level.Progress.LegacyCollectionQuotaExempt == saveBaseline.LegacyCollectionQuotaExempt &&
                fragment.IsCollected == (fragmentCollectedEvents == 1) &&
                fragment.FeedbackCount == entryFragmentFeedback + fragmentCollectedEvents &&
                fragmentVisual.Visible == (fragmentCollectedEvents == 0) &&
                hud.CurrentModel.FragmentCount == entryFragments.Length + fragmentCollectedEvents &&
                hud.CurrentModel.FragmentRequired == 6 && !hud.CurrentModel.CollectionQuotaExempt,
                "only one verified fragment transaction changes IDs/Save; all original values remain except the witnessed supply discovery");
            if (keyboardRoute)
            {
                KeyboardSource();
                Check(!Input.IsActionPressed(InputActions.MoveDown) &&
                    (!Input.IsActionPressed(InputActions.MoveUp) || keyboardAimHeld &&
                        (phase == "beam-prepare" || phase == "beam-cast") && !spaceIssued) &&
                    (!Input.IsActionPressed(InputActions.Claw) || keyboardCastHeld && castIssued && phase == "beam-cast") &&
                    keyboardDriver.ClawEdges >= entryKeyboardClaws && keyboardDriver.ClawEdges <= entryKeyboardClaws + (castIssued ? 1 : 0),
                    "only the owned W/K preparation/admission window may hold up or claw; no S or extra K");
            }
            else Check(!Input.IsActionPressed(InputActions.MoveUp) && !Input.IsActionPressed(InputActions.MoveDown),
                "legacy route never uses W/S");
            Check(!input.CurrentFrame.DashPressed && !input.CurrentFrame.ShieldHeld && !input.CurrentFrame.AttackModePressed &&
                (!(input.CurrentFrame.JumpPressed || input.CurrentFrame.JumpHeld) || spaceIssued),
                "uses only bounded movement, one cast and requested genuine Space; never dash/shield/combat");
            Check(claw.CurrentFrame.Failure == MechanicalClawFailure.None &&
                (claw.CurrentFrame.CastId == 0 || castIssued && claw.CurrentFrame.CastId == nextCast) &&
                (claw.CurrentFrame.NextCastId == nextCast || castIssued && claw.CurrentFrame.NextCastId == nextCast + 1),
                "allows one exact new cast and no failed/retry cast");
            Progress();
        }
        void KeyboardSource()
        {
            Check(input.CurrentFrame.KeyboardActions &&
                !input.CurrentFrame.PrimaryPressed && !input.CurrentFrame.PrimaryHeld && !input.CurrentFrame.PrimaryReleased &&
                !input.CurrentFrame.GrappleModePressed && !input.CurrentFrame.AttackModePressed && !input.CurrentFrame.LegacyShieldHeld &&
                !Input.IsActionPressed(InputActions.PrimaryAction) && !Input.IsActionPressed(InputActions.SelectGrappleMode) &&
                !Input.IsActionPressed(InputActions.SelectAttackMode) && !Input.IsActionPressed(InputActions.Shield) &&
                !input.CurrentFrame.AttackPressed && !Input.IsActionPressed(InputActions.Attack) &&
                !Input.IsPhysicalKeyPressed(Key.J) && keyboardDriver.AttackEdges == entryKeyboardAttacks &&
                !input.CurrentFrame.InteractPressed && !Input.IsActionPressed(InputActions.Interact),
                "real keyboard source has no mouse/legacy modes/primary, J attack or Q interaction in the supply detour");
        }
        void ObserveCastInput()
        {
            bool pressed = keyboardRoute ? input.CurrentFrame.ClawPressed : input.CurrentFrame.PrimaryPressed;
            if (!pressed || input.CurrentFrameSequence == castSequence) return;
            castSequence = input.CurrentFrameSequence; castSamples++;
            Check(castIssued && castSequence > castIssueSequence && castSamples == 1,
                keyboardRoute ? "one fresh accepted K only after the owned cast issue" : "samples one real LMB only");
            if (keyboardRoute)
            {
                KeyboardSource();
                Check(keyboardCastHeld && keyboardAimHeld && !spaceIssued && phase == "beam-cast" &&
                    keyboardReadySequence != 0 && castIssueSequence >= keyboardReadySequence &&
                    Input.IsPhysicalKeyPressed(Key.K) && Input.IsPhysicalKeyPressed(Key.W) &&
                    input.CurrentFrame.MoveAxis == 0 && input.KeyboardAimDirection.Dot(Vector2.Up) > .999f &&
                    keyboardDriver.ClawEdges == entryKeyboardClaws + 1,
                    "accepted K owns one physical W+K edge after actual upward readiness");
            }
        }
        void Guard()
        {
            Safety(); previousGuard?.Invoke();
            ObserveCastInput();
            if (input.CurrentFrame.JumpPressed && input.CurrentFrameSequence != spaceSequence)
            {
                spaceSequence = input.CurrentFrameSequence; spaceSamples++;
                Check(spaceIssued && spaceSamples == 1, "samples one actual attached Space edge only");
            }
        }
        void Pre() { Safety(); previousPre?.Invoke(); }
        async Task Tick()
        {
            Safety();
            await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
            Native();
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            Safety(); waits++; observe(); // Original combined observer, after motor/deferred commit, exactly once.
            Check(fragmentCollectedEvents == 0 || collectionBodyEntries == 1,
                "post-process collection has its independent same-player native BodyEntered receipt");
        }
        async Task Walk(float target, bool shelfRequired = false, bool preciseKeyboardStop = false,
            bool acquireShelfKeyboardStop = false)
        {
            Safety();
            int direction = target > player.GlobalPosition.X ? 1 : -1;
            StringName key = direction > 0 ? InputActions.MoveRight : InputActions.MoveLeft;
            SendAction(InputActions.MoveLeft, false); SendAction(InputActions.MoveRight, false);
            bool preciseStop = keyboardRoute && preciseKeyboardStop;
            bool landingStop = keyboardRoute && acquireShelfKeyboardStop;
            bool fullShelfAcquired = false, landingDirectionHeld = false;
            ulong landingReleaseSequence = input.CurrentFrameSequence;
            if (landingStop) shelfConsumerControl = "air-D-then-full-named-shelf-actual-neutral";
            bool StopSupported() => shelfRequired || landingStop ?
                Supported(shelf, shelfShape, 960, landingStop ? "landing-control-support" : null) : Supported(floor, floorShape, 1116);
            bool StopReady() => Mathf.Abs(target - player.GlobalPosition.X) <= 1.5f &&
                player.Velocity.Length() <= 2f && input.CurrentFrame.MoveAxis == 0f &&
                !Input.IsPhysicalKeyPressed(Key.A) && !Input.IsPhysicalKeyPressed(Key.D) &&
                StopSupported();
            bool TargetReady() => StopReady() && (!landingStop || fullShelfAcquired &&
                input.CurrentFrameSequence > landingReleaseSequence);
            if (!preciseStop) SendAction(key, true);
            RecordShelfMotion("walk-issued", target, 96);
            try
            {
                for (int i = 0; i < 90 && (preciseStop || landingStop ? !TargetReady() :
                    (target - player.GlobalPosition.X) * direction > 2); i++)
                {
                    if (landingStop && !fullShelfAcquired &&
                        Supported(shelf, shelfShape, 960, "landing-acquire"))
                    {
                        fullShelfAcquired = true;
                        RecordShelfMotion("actual-full-named-shelf-acquired", target, 90 - i);
                    }
                    if (landingStop && fullShelfAcquired)
                    {
                        var stop = FreeMotionForecast.Neutral(player.Velocity.X, true, motor.MotionSettings,
                            motor.ThrusterState.Module, 1d / Engine.PhysicsTicksPerSecond, 90 - i, 2f);
                        Check(stop.SpeedThresholdReached, "shelf neutral endpoint fits only unused original Walk90");
                        float stopError = target - (player.GlobalPosition.X + stop.NetDistance);
                        int steer = stopError > 1.5f ? 1 : stopError < -1.5f ? -1 : 0;
                        SendAction(InputActions.MoveLeft, steer < 0);
                        SendAction(InputActions.MoveRight, steer > 0);
                        bool held = steer != 0;
                        if (held || landingDirectionHeld) landingReleaseSequence = input.CurrentFrameSequence;
                        landingDirectionHeld = held;
                    }
                    else if (preciseStop)
                    {
                        // The lower native-ray stance and narrow shelf return both
                        // brake through real A/D/neutral, not a one-sided overshoot.
                        float stanceDelta = target - player.GlobalPosition.X;
                        float vx = player.Velocity.X;
                        float braking = vx * vx / (2f * motor.Tuning.GroundDeceleration) +
                            Mathf.Abs(vx) / Engine.PhysicsTicksPerSecond;
                        int steer = Mathf.Abs(stanceDelta) <= 1.5f ? 0 : MathF.Sign(stanceDelta);
                        if (steer != 0 && vx * stanceDelta > 0f && Mathf.Abs(stanceDelta) <= braking + 1.5f)
                            steer = 0;
                        SendAction(InputActions.MoveLeft, steer < 0);
                        SendAction(InputActions.MoveRight, steer > 0);
                    }
                    await Tick();
                    RecordShelfMotion($"walk-tick-{i}", target, 90 - i - 1 + 6);
                    if (preciseStop || landingStop && fullShelfAcquired) Check(StopSupported(),
                        "selected keyboard stop stays fully supported on its original support during real A/D braking");
                    if (shelfRequired) Check(Supported(shelf, shelfShape, 960) && gate.IsOpen &&
                        gate.Frame.Traversable && gateShape.Disabled && DoorClear(), "ordinary chamber crossing stays supported through real open door");
                }
                Check(preciseStop || landingStop ? TargetReady() : (target - player.GlobalPosition.X) * direction <= 2,
                    $"ordinary bounded walk reaches x{target}");
            }
            finally
            {
                SendAction(key, false);
                if (preciseStop || landingStop)
                { SendAction(InputActions.MoveLeft, false); SendAction(InputActions.MoveRight, false); }
                RecordShelfMotion("walk-key-released", target, 6);
            }
            for (int i = 0; i < 6; i++)
            {
                await Tick();
                RecordShelfMotion($"neutral-tail-{i}", target, 6 - i - 1);
                if (preciseStop || landingStop) Check(StopSupported(),
                    "original six neutral settle samples keep full selected support");
            }
            if (preciseStop || landingStop) Check(TargetReady(), "selected keyboard stop retains its actual stopped narrow stance after the original settle budget");
            if (shelfRequired) Check(Supported(shelf, shelfShape, 960), "neutral chamber walk remains supported");
        }
        void Disk(bool spent)
        {
            Check(File.Exists(savePath) && snapshotBytes.SequenceEqual(File.ReadAllBytes(snapshotPath)),
                "automatic case save exists and retained pre-Boss bytes stay immutable");
            var saved = PrologueSaveStore.LoadOrNew(savePath, 2);
            using JsonDocument wire = JsonDocument.Parse(File.ReadAllBytes(savePath));
            JsonElement raw = wire.RootElement;
            PrologueSaveData rawValues = Task18RawSave(File.ReadAllBytes(savePath));
            string expectedFragments = fragmentCollectedEvents == 0 ? entryFragmentIds : exitFragmentIds;
            string expectedDiscoveries = Task18Ids(spent ? discoveries.Append(rewardId) : discoveries);
            Check(Task18SaveExact(rawValues, saveBaseline, expectedFragments, expectedDiscoveries) &&
                Task18SaveExact(flow.Save, saveBaseline, expectedFragments, expectedDiscoveries),
                "raw disk/live all twenty values match original snapshot plus only independently witnessed fragment/supply deltas");
            Check(raw.GetProperty("version").GetInt32() == PrologueSaveData.CurrentVersion && raw.GetProperty("layoutVersion").GetInt32() == 2 &&
                raw.GetProperty("hasActivatedCheckpoint").GetBoolean() && raw.GetProperty("checkpointId").GetString() == checkpointId &&
                raw.GetProperty("checkpointSector").GetString() == "LostTemple" &&
                raw.GetProperty("checkpointWorldX").GetSingle() == 5920 && raw.GetProperty("checkpointWorldY").GetSingle() == 1080 &&
                raw.GetProperty("discoveries").EnumerateArray().Any(s => s.GetString() == rewardId) == spent,
                "raw automatic JSON independently records schema/layout/checkpoint and finite reward discovery");
            Check(!saved.WasRecovered && saved.Version == PrologueSaveData.CurrentVersion && saved.LayoutVersion == 2 &&
                saved.HasActivatedCheckpoint && saved.CheckpointId == checkpointId &&
                saved.CheckpointSector == "LostTemple" && saved.CheckpointWorldPosition == new Vector2(5920, 1080) &&
                saved.OpenShortcuts.OrderBy(s => s, StringComparer.Ordinal).SequenceEqual(shortcuts) &&
                saved.ActivatedMechanisms.OrderBy(s => s, StringComparer.Ordinal).SequenceEqual(mechanisms) &&
                saved.Discoveries.Contains(rewardId) == spent &&
                saved.Discoveries.Where(s => s != rewardId).OrderBy(s => s, StringComparer.Ordinal).SequenceEqual(discoveries) &&
                flow.Save.Discoveries.Contains(rewardId) == spent && !saved.BossDefeated && !saved.Completed,
                "actual automatic disk/live save retain checkpoint, upstream progress and exact finite reward state");
        }
        void Callback(Action assertion, MechanicalClawFrame? publishedFrame = null, GrappleSurface2D publishedTarget = null)
        {
            try { Native(); assertion(); }
            catch (Exception error)
            {
                if (callbackFailure == null)
                {
                    // Failure-only witness before finally releases W/K. Keep the
                    // original assertions and all timing/admission bounds intact.
                    GD.Print($"TASK18_NORMAL_SUPPLY_FIRST_CALLBACK_FAILURE phase={phase} physics={Engine.GetPhysicsFrames()} " +
                        $"process={Engine.GetProcessFrames()} usec={Time.GetTicksUsec()} publication={publication} " +
                        $"error={JsonSerializer.Serialize(error.Message)} " +
                        $"focused={tree.Root.HasFocus()} sample={input.CurrentFrameSequence} " +
                        $"ready={keyboardReadySequence} issue={castIssueSequence} accepted={castSequence} samples={castSamples} " +
                        $"issued={castIssued} held_WK={keyboardAimHeld}/{keyboardCastHeld} " +
                        $"physical_WK={Input.IsPhysicalKeyPressed(Key.W)}/{Input.IsPhysicalKeyPressed(Key.K)} " +
                        $"move={input.CurrentFrame.MoveAxis} aim={input.KeyboardAimDirection} " +
                        $"sampled_K_pressed={input.CurrentFrame.ClawPressed} " +
                        $"driver_edges={entryKeyboardClaws}/{keyboardDriver?.ClawEdges} " +
                        $"state={claw.CurrentFrame.State} cast={claw.CurrentFrame.CastId}/{claw.CurrentFrame.NextCastId} " +
                        $"target={(Live(claw.CurrentTarget) ? claw.CurrentTarget.GetInstanceId() : 0)} " +
                        $"preview={(Live(claw.KeyboardPreview.Surface) ? claw.KeyboardPreview.Surface.GetInstanceId() : 0)} " +
                        $"locked={claw.CurrentFrame.LockedTargetRuntimeId} expected_beam={beamId} " +
                        $"anchor={claw.CurrentFrame.AnchorPoint} normal={claw.CurrentFrame.SurfaceNormal} " +
                        $"event_frame_present={publishedFrame.HasValue} event_state={publishedFrame?.State} " +
                        $"event_cast={publishedFrame?.CastId}/{publishedFrame?.NextCastId} " +
                        $"event_locked={publishedFrame?.LockedTargetRuntimeId} " +
                        $"event_anchor={publishedFrame?.AnchorPoint} event_normal={publishedFrame?.SurfaceNormal} " +
                        $"event_target={(Live(publishedTarget) ? publishedTarget.GetInstanceId() : 0)} " +
                        $"event_is_beam={Live(publishedTarget) && publishedTarget == beam}");
                }
                callbackFailure = callbackFailure == null ? error : new AggregateException(callbackFailure, error);
            }
        }
        bool FragmentOverlap()
        {
            Rect2 body = Bounds();
            float radius = body.Size.X * .5f;
            float spineHalf = body.Size.Y * .5f - radius;
            Vector2 center = body.GetCenter();
            Vector2 nearest = new(center.X, Mathf.Clamp(fragmentTransform.Origin.Y, center.Y - spineHalf, center.Y + spineHalf));
            return fragment.GetOverlappingBodies().Contains(player) &&
                nearest.DistanceTo(fragmentTransform.Origin) <= radius + fragmentCircle.Radius;
        }
        void OnFragmentCollected(string stableId) => Callback(() =>
        {
            fragmentCollectedEvents++;
            Check(fragmentCollectedEvents == 1 && phase == "shelf-landing" && stableId == fragmentId &&
                fragment.GetInstanceId() == fragmentNodeId && fragment.IsCollected &&
                fragment.LastResult == FragmentCollectionResult.Collected &&
                fragment.FeedbackCount == entryFragmentFeedback + 1 && !fragmentVisual.Visible && FragmentOverlap() &&
                level.Player == player && player.GetInstanceId() == playerId && health.CurrentHealth == entryHp &&
                heals == 0 && persists == 0 && saveReplacements == 0 &&
                ReferenceEquals(acceptedSave, initialSave) && !ReferenceEquals(flow.Save, initialSave) &&
                Task18SaveExact(initialSave, saveBaseline, entryFragmentIds, Task18Ids(discoveries)) &&
                Task18Ids(level.Progress.CollectedFragmentIds) == exitFragmentIds &&
                !level.Progress.LegacyCollectionQuotaExempt && hud.CurrentModel.FragmentCount == entryFragments.Length + 1 &&
                hud.CurrentModel.FragmentRequired == 6 && !hud.CurrentModel.CollectionQuotaExempt,
                "unique real Collected follows native radius18 overlap/Feedback/HUD and only the exact temple-upper +1 transaction");
            // Production emits Collected inside its BodyEntered handler, before our
            // later BodyEntered listener. Do not invent the opposite callback order.
            Disk(false);
            collectionDiskHash = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(savePath)));
            acceptedSave = flow.Save; saveReplacements++;
            Log("FRAGMENT_COLLECTED");
        });
        void OnFragmentBody(Node2D body) => Callback(() =>
        {
            Check(Live(body) && body == player && body.GetInstanceId() == playerId && FragmentOverlap(),
                "fragment BodyEntered is the same original player with actual native and capsule/circle overlap");
            if (phase == "shelf-landing")
            {
                collectionBodyEntries++; collectionBodyPlayerId = body.GetInstanceId();
                Check(collectionBodyEntries == 1 && returnBodyEntries == 0 && heals == 0 && persists == 0,
                    "one first shelf-landing body entry independently attributes the collection");
                Log("FRAGMENT_COLLECTION_BODY");
            }
            else
            {
                returnBodyEntries++; returnBodyPlayerId = body.GetInstanceId();
                Check(phase == "return" && returnBodyEntries == 1 && collectionBodyEntries == 1 &&
                    fragmentCollectedEvents == 1 && saveReplacements == 1 && fragment.IsCollected &&
                    fragment.LastResult == FragmentCollectionResult.Collected &&
                    fragment.FeedbackCount == entryFragmentFeedback + 1 && !fragmentVisual.Visible &&
                    ReferenceEquals(acceptedSave, flow.Save) &&
                    Task18SaveExact(flow.Save, saveBaseline, exitFragmentIds, Task18Ids(discoveries.Append(rewardId))) &&
                    Task18Ids(level.Progress.CollectedFragmentIds) == exitFragmentIds &&
                    heals == 1 && persists == 1 && returnDiskHash.Length == 64 &&
                    Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(savePath))) == returnDiskHash,
                    "one natural return re-entry keeps collected state/Feedback/IDs/accepted Save and disk unchanged; no third/other-phase entry");
                Disk(true); Log("FRAGMENT_RETURN_BODY");
            }
        });
        void OnHealed(int restored, int remaining) => Callback(() =>
        {
            heals++;
            Check(heals == 1 && restored == expectedHp - entryHp && remaining == expectedHp &&
                health.CurrentHealth == expectedHp && reward.IsSpent && level.Progress.Discoveries.Contains(rewardId) &&
                reward.OverlapsBody(player) && phase == "reward-walk" && persists == 0,
                "real overlap consumes discovery before one exact capped +2 Healed event and before persist publication");
        });
        void OnBody(Node2D body) => Callback(() =>
        {
            Check(Live(body), "requires native-valid reward overlap body");
            if (body != player) return;
            overlaps++;
            Check(overlaps == 1 && body.GetInstanceId() == playerId && heals == 1 && persists == 1,
                "actual same-player reward BodyEntered follows the finite synchronous consumer");
        });
        void OnPersist(string reason) => Callback(() =>
        {
            if (reason != "discovery") return;
            persists++;
            Check(persists == 1 && heals == 1 && phase == "reward-walk", "one natural discovery persist follows real healing");
            Disk(true);
        });
        void OnClaw(MechanicalClawFrame frame, GrappleSurface2D target) => Callback(() =>
        {
            publication++;
            Check(frame.Failure == MechanicalClawFailure.None && frame.State != MechanicalClawState.Failed &&
                (frame.CastId == 0 || castIssued && frame.CastId == nextCast), "all publications retain one valid requested cast");
            if (keyboardRoute && frame.State == MechanicalClawState.Aim)
            {
                ObserveCastInput();
                Check(castSamples == 1 && castSequence > castIssueSequence &&
                    Live(target) && target == beam && claw.CurrentTarget == beam &&
                    claw.KeyboardPreview.Surface == beam && claw.KeyboardPreview.Snapshot.RuntimeId == beamId &&
                    claw.KeyboardPreview.Snapshot.Exists && claw.KeyboardPreview.Snapshot.HasLineOfSight &&
                    !claw.KeyboardPreview.Snapshot.IsLegacyInteraction &&
                    frame.CastId == 0 && frame.NextCastId == nextCast && frame.EffectiveVisibility &&
                    frame.AnchorPoint.IsFinite() && frame.AnchorPoint.X >= 5805 && frame.AnchorPoint.X <= 5855 &&
                    Mathf.Abs(frame.AnchorPoint.Y - 860) < 1 && frame.SurfaceNormal.Y > .7f &&
                    Supported(floor, floorShape, 1116),
                    "fresh W+K Aim uses the production-selected beam underside from full lower support");
            }
            if (keyboardRoute && frame.State == MechanicalClawState.Windup)
                Check(castSamples == 1 && Live(target) && target == beam && frame.CastId == nextCast &&
                    frame.NextCastId == nextCast + 1, "one accepted K assigns the sole beam cast token at Windup");
            if (frame.State is MechanicalClawState.Travel or MechanicalClawState.Latched or MechanicalClawState.Reel or MechanicalClawState.Swing)
            {
                Check(Live(target) && target == beam && frame.LockedTargetRuntimeId == beamId &&
                    frame.AnchorPoint.X >= 5805 && frame.AnchorPoint.X <= 5855 &&
                    Mathf.Abs(frame.AnchorPoint.Y - 860) < 1 && frame.SurfaceNormal.Y > .7f,
                    "real cast attaches only to intended nonlegacy beam underside");
                if (frame.State == MechanicalClawState.Travel) travel = true;
                if (frame.State == MechanicalClawState.Latched) { Check(travel, "Travel precedes Latched"); latched = true; }
                if (frame.Arrived && !arrived)
                {
                    Check(latched && frame.State == MechanicalClawState.Swing && !spaceIssued,
                        "first true Arrived is Swing on the same cast before Space");
                    arrived = true; arrivalTick = Engine.GetPhysicsFrames();
                }
            }
            if (frame.State == MechanicalClawState.Boost)
            {
                boosts++; boostPublication = publication; boostTick = Engine.GetPhysicsFrames();
                Check(spaceIssued && arrived && boosts == 1 && releases == 0 && frame.LockedTargetRuntimeId == beamId &&
                    frame.BoostConsumed && frame.ReleaseKind == MechanicalClawReleaseKind.Boost,
                    "one requested Space publishes genuine consumed Boost");
                if (keyboardRoute) Log("BOOST_PUBLICATION_PRE_MOTOR");
            }
            if (frame.State == MechanicalClawState.Released)
            {
                releases++;
                Check(boosts == 1 && releases == 1 && publication == boostPublication + 1 &&
                    boostTick == Engine.GetPhysicsFrames() && boostTick == spaceTick + 1 &&
                    frame.LockedTargetRuntimeId == beamId && frame.BoostConsumed &&
                    frame.ReleaseKind == MechanicalClawReleaseKind.Boost,
                    "one same-tick adjacent Boost/Released transaction; no Cancel substitute");
            }
            if (frame.State == MechanicalClawState.Idle && castIssued)
            { Check(releases == 1 && frame.CastId == 0 && frame.NextCastId == nextCast + 1, "release reaches natural Idle"); idle = true; }
        }, frame, target);
        void Log(string boundary)
        {
            Native();
            GD.Print($"TASK18_NORMAL_SUPPLY boundary={boundary} phase={phase} physics={Engine.GetPhysicsFrames()} " +
                $"player_id={playerId} level_id={level.GetInstanceId()} world_id={world.GetInstanceId()} temple_id={temple.GetInstanceId()} " +
                $"boss_id={bossId} beam_id={beamId} reward_id={reward.GetInstanceId()} floor_id={floor.GetInstanceId()} shelf_id={shelf.GetInstanceId()} " +
                $"hp={entryHp}->{health.CurrentHealth}/{maxHp} recovery={requests}/{recoveries}->{monitor.RecoveryRequested}/{monitor.RecoveryCompleted} " +
                $"jumps={jumps}->{motor.AcceptedJumpCount} dashes={dashes}->{motor.AcceptedDashCount} chain_apps={applications}->{adapter.ApplicationCount} " +
                $"checkpoint={world.CheckpointId} boss_hp={bossHp}->{bossHealth.CurrentHealth} actors={enemyIds.Length} " +
                $"next_cast={nextCast}->{claw.CurrentFrame.NextCastId} cast={claw.CurrentFrame.CastId} " +
                $"anchor={claw.CurrentFrame.AnchorPoint} normal={claw.CurrentFrame.SurfaceNormal} dock={claw.CurrentFrame.DockPoint} " +
                $"life={travel}/{latched}/{arrived}/{boosts}/{releases}/{idle} position={player.GlobalPosition} " +
                $"velocity={player.Velocity} body={Bounds()} heals={heals} overlaps={overlaps} persists={persists} " +
                $"fragment={fragmentId}/{fragmentNodeId} fragment_shape={fragmentShapeId} fragment_position={fragment.GlobalPosition} " +
                $"fragment_bodies={collectionBodyEntries}/{returnBodyEntries} fragment_collected={fragmentCollectedEvents} " +
                $"fragment_feedback={entryFragmentFeedback}->{fragment.FeedbackCount} save_replacements={saveReplacements} " +
                $"fragment_count={entryFragments.Length}->{level.Progress.CollectedFragmentIds.Count} collection_disk_sha={collectionDiskHash} " +
                $"input_source={(keyboardRoute ? "keyboard-K" : "legacy-LMB")} input_edges={castSamples}/{spaceSamples} " +
                $"move={input.CurrentFrame.MoveAxis} keyboard_aim={input.KeyboardAimDirection} " +
                $"raw_WADSpace={Input.IsPhysicalKeyPressed(Key.W)}/{Input.IsPhysicalKeyPressed(Key.A)}/" +
                $"{Input.IsPhysicalKeyPressed(Key.D)}/{Input.IsPhysicalKeyPressed(Key.Space)} " +
                $"keyboard_ready={keyboardReadySequence} cast_issue_sample={castIssueSequence}/{castSequence} " +
                $"timing={arrivalTick}/{spaceTick}/{boostTick} waits={waits}");
        }

        Task18NormalSupplyReceipt receipt = null;
        try
        {
            previousGuard = swapApproachGuard(Guard); guardInstalled = true;
            previousPre = swapPreObserverGuard(Pre); preInstalled = true;
            claw.ClawFrameChanged += OnClaw; clawSubscribed = true;
            health.Healed += OnHealed; healthSubscribed = true;
            reward.BodyEntered += OnBody; rewardSubscribed = true;
            level.ProgressPersistRequested += OnPersist; persistSubscribed = true;
            fragment.BodyEntered += OnFragmentBody; fragmentBodySubscribed = true;
            fragment.Collected += OnFragmentCollected; fragmentCollectedSubscribed = true;
            Safety(); Disk(false);
            Check(Idle() && Supported(floor, floorShape, 1116), "starts from natural Idle on full original CheckpointFloor");
            Check(!gate.IsOpen && !gate.HasOpened && !gateShape.Disabled && !DoorClear(),
                "measures initially closed supply gate/query before genuine plate opening");
            Log("ENTRY"); phase = "lower-approach";
            await Walk(5825, preciseKeyboardStop: keyboardRoute);
            Check(Idle() && Supported(floor, floorShape, 1116), "ordinary lower walking reaches fixed beam approach");
            if (keyboardRoute) Log("KEYBOARD_LOWER_STANCE");
            MechanicalClawTargetResolution resolved;
            if (keyboardRoute)
            {
                // W is an actual sampled direction, not the old mouse planner point.
                // The original approach is under the beam; no horizontal repositioning is needed.
                phase = "beam-prepare";
                ulong readyIssue = input.CurrentFrameSequence;
                keyboardAimHeld = true; SendAction(InputActions.MoveUp, true);
                bool Ready() => input.CurrentFrameSequence > readyIssue &&
                    input.CurrentFrame.MoveAxis == 0 && input.KeyboardAimDirection.Dot(Vector2.Up) > .999f &&
                    Input.IsPhysicalKeyPressed(Key.W) && !Input.IsPhysicalKeyPressed(Key.K) &&
                    !Input.IsActionPressed(InputActions.MoveLeft) && !Input.IsActionPressed(InputActions.MoveRight) &&
                    Idle() && modes.CanBeginGrapple && Supported(floor, floorShape, 1116) &&
                    Mathf.Abs(player.Velocity.X) <= 2f;
                for (int i = 0; i < 8 && !Ready(); i++) await Tick();
                KeyboardSource();
                Check(Ready() && keyboardDriver.ClawEdges == entryKeyboardClaws,
                    "fresh physical W prepares upward claw on full lower support within8, before any K");
                keyboardReadySequence = input.CurrentFrameSequence;
                resolved = resolver.ResolveKeyboard(player, input.KeyboardAimDirection);
            }
            else resolved = resolver.Resolve(player, aim);
            Check(beam.IsAvailable && IsTargetVisible(player, resolved.Snapshot.WorldAnchor) &&
                resolved.Surface == beam && resolved.Snapshot.Exists && resolved.Snapshot.RuntimeId == beamId &&
                !resolved.Snapshot.IsLegacyInteraction && resolved.Snapshot.HasLineOfSight &&
                player.GlobalPosition.DistanceTo(resolved.Snapshot.WorldAnchor) <= MechanicalClawSettings.Default.MaxRange,
                keyboardRoute ? "actual sampled W resolves the exact visible in-range LOS nonlegacy beam" :
                    "fixed world aim resolves exact visible in-range LOS nonlegacy surface");
            phase = "beam-cast";
            if (!keyboardRoute)
            {
                MoveMouse(player, aim);
                ulong eIssue = input.CurrentFrameSequence;
                SendAction(InputActions.SelectGrappleMode, true);
                await Tick(); SendAction(InputActions.SelectGrappleMode, false);
                Check(input.CurrentFrameSequence > eIssue && input.CurrentFrame.GrappleModePressed && modes.CanBeginGrapple &&
                    Idle() && input.CurrentFrame.MouseWorld.DistanceTo(aim) <= .01f, "real fresh E/aim precedes the single LMB");
            }
            castIssueSequence = input.CurrentFrameSequence;
            ulong castRequest = Engine.GetPhysicsFrames();
            int castWaits = 0;
            castIssued = true;
            if (keyboardRoute)
            {
                keyboardCastHeld = true;
                try
                {
                    SendAction(InputActions.Claw, true);
                    for (int i = 0; i < 8; i++)
                    {
                        await Tick(); castWaits++;
                        if (castSequence > castIssueSequence && claw.CurrentFrame.State is
                            MechanicalClawState.Aim or MechanicalClawState.Windup or MechanicalClawState.Travel) break;
                    }
                    Safety();
                    Check(castSamples == 1 && castSequence > castIssueSequence &&
                        Input.IsPhysicalKeyPressed(Key.K) && Input.IsPhysicalKeyPressed(Key.W) &&
                        input.CurrentFrame.MoveAxis == 0 && input.KeyboardAimDirection.Dot(Vector2.Up) > .999f &&
                        claw.CurrentTarget == beam && claw.CurrentFrame.State is
                            MechanicalClawState.Aim or MechanicalClawState.Windup or MechanicalClawState.Travel,
                        "one fresh physical W+K reaches production beam admission within8; no early directional release");
                    Log("KEYBOARD_CAST_ADMITTED");
                }
                finally
                {
                    SendAction(InputActions.Claw, false); keyboardCastHeld = false;
                    SendAction(InputActions.MoveUp, false); keyboardAimHeld = false;
                }
            }
            else
            {
                SendMouse(MouseButton.Left, true, player, aim);
                await Tick(); castWaits++;
                SendMouse(MouseButton.Left, false, player, aim);
            }
            // Admission waits consume the original cast budget; never add eight to 300.
            while (castWaits < 300 && !arrived && Engine.GetPhysicsFrames() - castRequest < 300)
            { await Tick(); castWaits++; }
            Safety();
            Check(travel && latched && arrived && castSamples == 1 && castSequence > castIssueSequence &&
                claw.CurrentFrame.Arrived && claw.CurrentFrame.State == MechanicalClawState.Swing &&
                claw.CurrentFrame.CastId == nextCast && arrivalTick == Engine.GetPhysicsFrames() && arrivalTick <= castRequest + 300,
                "one exact cast reaches first post-motor true Swing Arrived within300 without timing search");
            Log("ARRIVED"); phase = "space-release";
            spaceTick = Engine.GetPhysicsFrames(); spaceIssued = true;
            if (keyboardRoute) Log("SPACE_REQUEST_POST_MOTOR");
            SendAction(InputActions.MoveRight, true); SendAction(InputActions.Jump, true); // fixed delay0, same dispatch batch
            for (int i = 0; i < 3; i++) await Tick();
            SendAction(InputActions.Jump, false);
            Check(spaceTick == arrivalTick && boosts == 1 && releases == 1 && spaceSamples == 1,
                "delay0 D+Space gives one genuine Boost release and no ordinary jump");
            Log("BOOST_RELEASE"); phase = "shelf-landing";
            await Walk(5896, acquireShelfKeyboardStop: keyboardRoute);
            for (int i = 0; i < 90 && !Supported(shelf, shelfShape, 960, "wait-support"); i++) await Tick();
            for (int i = 0; i < 12; i++)
            {
                await Tick(); Rect2 b = Bounds();
                bool supported = Supported(shelf, shelfShape, 960, $"stable-index-{i}");
                bool leftInside = b.Position.X >= 5865, rightInside = b.End.X <= 5930;
                Vector2 platePoint = new(player.GlobalPosition.X, b.End.Y - .1f);
                bool plate = gate.PlateWorldRect.HasPoint(platePoint), opened = gate.HasOpened,
                    open = gate.IsOpen, traversable = gate.Frame.Traversable, doorDisabled = gateShape.Disabled;
                bool originalPrefix = supported && leftInside && rightInside && plate && opened && open && traversable && doorDisabled;
                bool? clear = originalPrefix ? DoorClear($"stable-index-{i}") : null;
                RecordShelf(() => $"use=stable-index-{i} timing=after-original-Physics-and-Process-Tick body={b} " +
                    $"position={player.GlobalPosition} velocity={player.Velocity} supported={supported} left_inside={leftInside} " +
                    $"right_inside={rightInside} plate_point={platePoint} plate_rect={gate.PlateWorldRect} plate_contains={plate} " +
                    $"gate_same_player={gate.PlayerBody == player} has_opened={opened} is_open={open} traversable={traversable} " +
                    $"door_disabled={doorDisabled} door_clear={(clear.HasValue ? clear.Value.ToString() : "not-evaluated-short-circuit")} " +
                    "query_count_unchanged=true");
                Check(originalPrefix && clear == true,
                    "12 consecutive full-capsule supported external shelf/grounded plate/committed clear door samples");
            }
            Log("PLATE_OPEN"); phase = "reward-walk";
            Check(collectionBodyEntries == 1 && fragmentCollectedEvents == 1 && saveReplacements == 1,
                "shelf arrival includes the unique independently witnessed temple-upper transaction before the supply");
            await Walk(6010, shelfRequired: true);
            Check(reward.OverlapsBody(player) && heals == 1 && overlaps == 1 && persists == 1 &&
                health.CurrentHealth == expectedHp && reward.IsSpent &&
                hud.CurrentModel.HealthSegments.Count(s => s == HealthShardState.Filled) == expectedHp,
                "real chamber overlap heals once and formal HUD filled shards match actual post-heal HP");
            Disk(true); Log("HEALED"); phase = "return";
            returnDiskHash = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(savePath)));
            await Walk(5896, shelfRequired: true, preciseKeyboardStop: keyboardRoute);
            Check(!reward.OverlapsBody(player), "physical reward exit precedes lower return");
            await Walk(5825);
            for (int i = 0; i < 90 && !Supported(floor, floorShape, 1116); i++) await Tick();
            ReleaseAll(player.GlobalPosition);
            ulong releaseSequence = input.CurrentFrameSequence;
            bool Neutral() => input.CurrentFrameSequence > releaseSequence && input.CurrentFrame.MoveAxis == 0 &&
                !input.CurrentFrame.PrimaryPressed && !input.CurrentFrame.PrimaryHeld && !input.CurrentFrame.PrimaryReleased &&
                !input.CurrentFrame.GrappleModePressed && !input.CurrentFrame.JumpPressed && !input.CurrentFrame.JumpHeld &&
                (!keyboardRoute || input.CurrentFrame.Aim == Vector2.Zero && !input.CurrentFrame.ClawPressed &&
                    !input.CurrentFrame.AttackPressed && !input.CurrentFrame.InteractPressed &&
                    !Input.IsActionPressed(InputActions.Claw) && !Input.IsPhysicalKeyPressed(Key.K) &&
                    !Input.IsActionPressed(InputActions.Attack) && !Input.IsPhysicalKeyPressed(Key.J)) &&
                player.Velocity.Length() <= 2;
            phase = "settle";
            for (int i = 0; i < 90 && !(Neutral() && Idle()); i++) await Tick();
            Safety(); Disk(true);
            Check(Neutral() && Idle() && idle && Supported(floor, floorShape, 1116) &&
                heals == 1 && boosts == 1 && releases == 1 && castSamples == 1 && spaceSamples == 1 &&
                (!keyboardRoute || keyboardDriver.ClawEdges == entryKeyboardClaws + 1 &&
                    keyboardDriver.AttackEdges == entryKeyboardAttacks) &&
                claw.CurrentFrame.NextCastId == nextCast + 1, "fresh neutral target-free natural Idle returns to full original lower support with exact finite deltas");
            Check(collectionBodyEntries == 1 && returnBodyEntries == 1 && fragmentCollectedEvents == 1 &&
                saveReplacements == 1 && collectionBodyPlayerId == playerId && returnBodyPlayerId == playerId &&
                fragment.FeedbackCount == entryFragmentFeedback + 1 && collectionDiskHash.Length == 64,
                "exact one collection and one non-consuming return BodyEntered; one Feedback/+ID/durable Save replacement only");
            Log("RETURN");
            receipt = new Task18NormalSupplyReceipt(playerId, bossId, entryHp, health.CurrentHealth, maxHp,
                nextCast, claw.CurrentFrame.NextCastId, requests, recoveries, jumps, dashes,
                adapter.ApplicationCount, heals, boosts, releases, snapshotHash,
                new Task18FragmentReceipt(fragmentId, fragmentNodeId, fragmentShapeId, fragment.GlobalPosition, fragmentCircle.Radius,
                    entryFragmentIds, exitFragmentIds, entryFragments.Length, level.Progress.CollectedFragmentIds.Count,
                    entryFragmentFeedback, fragment.FeedbackCount, collectionBodyEntries, returnBodyEntries,
                    fragmentCollectedEvents, saveReplacements, collectionBodyPlayerId, returnBodyPlayerId, collectionDiskHash));
        }
        catch (Exception error) { firstFailure = error; }
        finally
        {
            void Cleanup(Action action) { try { action(); } catch (Exception error) { cleanupFailures.Add(error); } }
            Cleanup(() => { if (callbackFailure != null) throw new InvalidOperationException("Normal supply late publication failure", callbackFailure); });
            Cleanup(() => { if (clawSubscribed && GodotObject.IsInstanceValid(claw)) claw.ClawFrameChanged -= OnClaw; });
            Cleanup(() => { if (healthSubscribed && GodotObject.IsInstanceValid(health)) health.Healed -= OnHealed; });
            Cleanup(() => { if (rewardSubscribed && GodotObject.IsInstanceValid(reward)) reward.BodyEntered -= OnBody; });
            Cleanup(() => { if (persistSubscribed && GodotObject.IsInstanceValid(level)) level.ProgressPersistRequested -= OnPersist; });
            Cleanup(() => { if (fragmentBodySubscribed && GodotObject.IsInstanceValid(fragment)) fragment.BodyEntered -= OnFragmentBody; });
            Cleanup(() => { if (fragmentCollectedSubscribed && GodotObject.IsInstanceValid(fragment)) fragment.Collected -= OnFragmentCollected; });
            Cleanup(() => ReleaseAll(Live(player) ? player.GlobalPosition : Vector2.Zero));
            Cleanup(() => { if (preInstalled) swapPreObserverGuard(previousPre); });
            Cleanup(() => { if (guardInstalled) swapApproachGuard(previousGuard); });
            Cleanup(() =>
            {
                foreach (string row in shelfDiagnosticRows) GD.Print(row);
                GD.Print($"TASK18_SHELF_DIAGNOSTIC_END rows={shelfDiagnosticRows.Count}/256 omitted={shelfDiagnosticOmitted} " +
                    $"faults={shelfDiagnosticFaults} first_fault={shelfDiagnosticFirstFault} consumer_control={shelfConsumerControl} " +
                    "recorder_additional_queries=0 extra_waits=0");
            });
        }
        if (firstFailure != null || cleanupFailures.Count != 0)
        {
            if (firstFailure != null) cleanupFailures.Insert(0, firstFailure);
            throw new AggregateException("Task18 normal supply phase=" + phase, cleanupFailures);
        }
        return receipt ?? throw new InvalidOperationException("Normal supply returned no measured receipt");
    }
}
