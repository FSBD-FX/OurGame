using System;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;
using Knight.Abilities;
using Knight.Combat;
using Knight.Player;
using Knight.Prologue;
using Knight.Prologue.Seamless;
using Knight.Save;
using Knight.Shared;
using Knight.Tests.Presentation;

namespace Knight.Tests.Integration;

public static partial class PrologueCanyonGeneratedMainRouteTests
{
    // Catches a return route that only works with a position reset, a different player,
    // a false K target/LOS, partial ledge support, healing or production recovery.
    // This is a controlled current-schema checkpoint fixture, NOT a Title-to-door run.
    internal static async Task RunFragmentReturnProbe(SceneTree tree, string stage = "temple-reverse")
    {
        bool basin = stage == "basin-abyss";
        bool six = stage == "temple-six";
        bool chain = stage == "temple-basin-abyss" || six;
        TestAssert.True(basin || chain || stage == "temple-reverse", "return fixture requires a known stage");
        GD.Print(basin
            ? "QUEST_FRAGMENT_RETURN_SCOPE stage=basin-abyss fixture=true startedFromTitle=false normalOutboundMisses=false seededCheckpoint=true seededMechanism=false seededShortcut=false seededBellChain=false collectedIdsInjected=false routeTeleport=false fullRoute=false"
            : $"QUEST_FRAGMENT_RETURN_SCOPE stage={stage} fixture=true startedFromTitle=false normalOutboundMisses=false seededCheckpoint=true seededMechanism=true seededShortcut=true seededBellChain=true collectedIdsInjected=false routeTeleport=false fullRoute=false");
        Task10ApplicationPersistenceGuard persistence = Task10ApplicationPersistenceGuard.CreateFromArguments();
        PrologueLevelController level = null;
        KeyboardRouteInputDriver keyboard = null;
        RouteMonitor monitor = null;
        Exception failure = null;
        int sessionStarts = 0, requests = 0, recoveries = 0;
        void Started() => sessionStarts++;
        void Requested(string reason)
        {
            requests++;
            if (basin) GD.Print($"QUEST_FRAGMENT_BASIN_RECOVERY_REQUEST reason={reason} position={level?.Player?.GlobalPosition}");
        }
        void Recovered(string reason) => recoveries++;
        try
        {
            level = GD.Load<PackedScene>(MainPath).Instantiate<PrologueLevelController>();
            level.Name = basin ? "QuestFragmentBasinAbyssFixture" : "QuestFragmentTempleReverseFixture";
            PrologueGameFlowController flow = ConfigureTask10Persistence(level, persistence);
            flow.HeadlessAutoStart = false;
            flow.AutomationAutoStart = false;
            TestAssert.True(!flow.BasinQualitySampleMode, "fragment return uses the saved normal main, not the sample");

            // Only approved fixture prerequisites are seeded. The fragment list and
            // collection metadata remain the untouched defaults of a new schema4 save.
            PrologueSaveData seed = PrologueSaveData.NewLayoutV2();
            seed.CheckpointId = basin ? "canyon.v2.basin-camp" : "canyon.v2.temple-forecourt";
            if (!basin)
            {
                seed.ActivatedMechanisms.Add(CanyonWorldLayout.CourtyardSealMechanismId);
                seed.OpenShortcuts.Add(CanyonWorldLayout.UpperBasinShortcutId);
                // The normal ground route opens this real door before reaching this
                // checkpoint. Disclose that fixture prerequisite, never clear the
                // live door or inject collection IDs to get the returning player past it.
                seed.OpenShortcuts.Add("canyon.v2.temple-bell-gate");
            }
            PrologueSaveStore.SaveAtomic(flow.SavePath, seed.Normalize());
            using (JsonDocument wire = JsonDocument.Parse(File.ReadAllBytes(flow.SavePath)))
            {
                JsonElement raw = wire.RootElement;
                TestAssert.True(raw.GetProperty("version").GetInt32() == 4 &&
                    raw.GetProperty("layoutVersion").GetInt32() == 2 &&
                    raw.GetProperty("collectionRuleVersion").GetInt32() == 1 &&
                    !raw.GetProperty("legacyCollectionQuotaExempt").GetBoolean() &&
                    raw.GetProperty("collectedFragmentIds").GetArrayLength() == 0 &&
                    raw.GetProperty("openShortcuts").GetArrayLength() == (basin ? 0 : 2) &&
                    (basin || raw.GetProperty("openShortcuts").EnumerateArray().Any(value => value.GetString() == "canyon.v2.temple-bell-gate")) &&
                    !raw.GetProperty("bossDefeated").GetBoolean() && !raw.GetProperty("completed").GetBoolean(),
                    "region fixture raw wire is current schema4/rule1, zero fragments, nonexempt and undefeated");
            }
            PrologueSaveData loaded = PrologueSaveStore.LoadOrNew(flow.SavePath, 2);
            TestAssert.True(!loaded.WasRecovered && loaded.CheckpointId == seed.CheckpointId &&
                loaded.CheckpointWorldPosition == (basin ? new Vector2(1680f, 3960f) : new Vector2(5920f, 1080f)) &&
                loaded.HasActivatedCheckpoint && loaded.CollectedFragmentIds.Count == 0 &&
                (basin ? loaded.ActivatedMechanisms.Count == 0 && loaded.OpenShortcuts.Count == 0 :
                    loaded.ActivatedMechanisms.SequenceEqual(new[] { CanyonWorldLayout.CourtyardSealMechanismId }) &&
                    loaded.OpenShortcuts.Count == 2 &&
                    loaded.OpenShortcuts.Contains(CanyonWorldLayout.UpperBasinShortcutId, StringComparer.Ordinal) &&
                    loaded.OpenShortcuts.Contains("canyon.v2.temple-bell-gate", StringComparer.Ordinal)),
                "fixture rereads its canonical real checkpoint and only its disclosed durable prerequisites");

            CharacterBody2D player = level.GetNode<CharacterBody2D>("PlayerRoot/Player");
            ulong playerId = player.GetInstanceId();
            TestAssert.Equal(new Vector2(160f, 600f), player.Position,
                "fixture never changes the authored pre-tree player pose");
            keyboard = KeyboardRouteInputDriver.Begin(player);
            Camera2D camera = player.GetNode<Camera2D>("Camera2D");
            camera.Enabled = true;
            flow.SessionStarted += Started;
            level.RecoveryRequested += Requested;
            level.RecoveryCompleted += Recovered;
            tree.Root.AddChild(level);
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            TestAssert.True(flow.State == PrologueGameFlowState.Title && flow.HasSave,
                "fixture initializes the real Title with valid current-save Continue eligibility");
            HealthComponent health = player.GetNode<HealthComponent>("Health");
            int titleHp = health.CurrentHealth;
            Button continuing = level.GetNode<Button>("ProductUi/MainMenu/Panel/Buttons/Continue");
            continuing.GrabFocus(); // UI focus only; gameplay entry still consumes physical Enter.
            TestAssert.True(continuing.HasFocus() && !continuing.Disabled, "fixture Continue owns actual keyboard focus");
            SendUiAccept(level, true);
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            SendUiAccept(level, false);
            for (int i = 0; i < 12 && flow.State != PrologueGameFlowState.Playing; i++)
            {
                await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
                await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            }
            TestAssert.True(flow.State == PrologueGameFlowState.Playing && sessionStarts == 1 &&
                player.GetInstanceId() == playerId && level.Player == player &&
                health.CurrentHealth == titleHp && titleHp > 0 && requests == 0 && recoveries == 0 &&
                flow.Save.CollectedFragmentIds.Count == 0 && level.Progress.CollectedFragmentIds.Count == 0 &&
                !flow.Save.LegacyCollectionQuotaExempt && !level.Progress.BossDefeated,
                "one physical Enter Continue initializes the same alive nonexempt zero-fragment player without recovery");
            SeamlessPrologueWorld world = level.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors");
            monitor = new RouteMonitor(level, player, camera, world, playerId, allowRouteBacktracking: true);
            if (basin)
                await DriveFragmentBasinAbyssReturn(tree, level, flow, world, player, monitor, keyboard,
                    titleHp, () => sessionStarts, () => requests, () => recoveries);
            else
            {
                await DriveFragmentTempleReverse(tree, level, flow, world, player, monitor, keyboard,
                    titleHp, () => sessionStarts, () => requests, () => recoveries);
                if (chain)
                {
                    await DriveFragmentSplitToCamp(tree, level, flow, world, player, monitor, keyboard,
                        titleHp, () => sessionStarts, () => requests, () => recoveries);
                    await DriveFragmentBasinAbyssReturn(tree, level, flow, world, player, monitor, keyboard,
                        titleHp, () => sessionStarts, () => requests, () => recoveries, chained: true);
                    TestAssert.True(player.GetInstanceId() == playerId && level.Player == player &&
                        level.Progress.CollectedFragmentIds.OrderBy(id => id, StringComparer.Ordinal).SequenceEqual(new[] {
                            "canyon.v2.fragment.abyss-floor", "canyon.v2.fragment.courtyard" }) &&
                        flow.Save.CheckpointId == "canyon.v2.temple-forecourt" && requests == 0 && recoveries == 0 && sessionStarts == 1,
                        "one continuous same-player fixture retains both actual collected IDs and original Temple checkpoint");
                    GD.Print($"QUEST_FRAGMENT_CHAIN_OK player={playerId} checkpoint={flow.Save.CheckpointId} fragments_before=0 fragments_after=2 segments=31 seededIds=false samePlayer=true noRecovery=true fullRoute=false");
                    if (six)
                        await DriveFragmentSixFromCamp(tree, level, flow, world, player, monitor, keyboard,
                            () => sessionStarts, () => requests, () => recoveries);
                }
            }
        }
        catch (Exception error)
        {
            failure = error;
        }
        finally
        {
            try
            {
                monitor?.Dispose();
                keyboard?.ReleaseAll();
                if (GodotObject.IsInstanceValid(level))
                {
                    level.RecoveryRequested -= Requested;
                    level.RecoveryCompleted -= Recovered;
                    level.GetNode<PrologueGameFlowController>("GameFlow").SessionStarted -= Started;
                    if (level.IsInsideTree()) await TestSceneTeardown.FreeSceneAfterAudioDrain(tree, level);
                    else level.Free();
                }
            }
            catch (Exception cleanup)
            {
                if (failure == null) failure = cleanup;
                else GD.Print($"QUEST_FRAGMENT_RETURN_CLEANUP_FAILURE type={cleanup.GetType().Name}");
            }
            finally
            {
                keyboard?.Dispose();
                try { persistence.AssertProtectedDefaultsUnchanged(); }
                catch (Exception guard)
                {
                    if (failure == null) failure = guard;
                    else GD.Print($"QUEST_FRAGMENT_RETURN_GUARD_FAILURE type={guard.GetType().Name}");
                }
            }
        }
        if (failure != null) ExceptionDispatchInfo.Capture(failure).Throw();
    }

    private static async Task DriveFragmentTempleReverse(SceneTree tree, PrologueLevelController level,
        PrologueGameFlowController flow, SeamlessPrologueWorld world, CharacterBody2D player,
        RouteMonitor monitor, KeyboardRouteInputDriver keyboard, int hp,
        Func<int> sessions, Func<int> requests, Func<int> recoveries)
    {
        PlayerInputReader input = player.GetNode<PlayerInputReader>("Input");
        PlayerMotor motor = player.GetNode<PlayerMotor>("Motor");
        HealthComponent health = player.GetNode<HealthComponent>("Health");
        MechanicalClawController claw = player.GetNode<MechanicalClawController>("MechanicalClaw");
        CollisionShape2D body = player.GetNode<CollisionShape2D>("BodyCollider");
        ulong playerId = player.GetInstanceId(), initialCast = claw.CurrentFrame.NextCastId;
        int jumps = motor.AcceptedJumpCount, dashes = motor.AcceptedDashCount;
        int rawK = keyboard.ClawEdges, rawJ = keyboard.AttackEdges, rawL = keyboard.ShieldEdges;
        int leftSamples = 0, kSamples = 0, segments = 0;
        ulong lastInput = input.CurrentFrameSequence;
        string phase = "initialization";
        bool Live(Node node) => node != null && GodotObject.IsInstanceValid(node) && node.IsInsideTree() && !node.IsQueuedForDeletion();
        string Facts() => $"phase={phase} physics={Engine.GetPhysicsFrames()} player={player.GlobalPosition} " +
            $"velocity={player.Velocity} floor={player.IsOnFloor()} hp={health.CurrentHealth}/{hp} " +
            $"recovery={requests()}/{recoveries()} input={input.CurrentFrameSequence}/{input.CurrentFrame.MoveAxis} " +
            $"claw={claw.CurrentFrame.State}/{claw.CurrentFrame.Failure} target=" +
            (GodotObject.IsInstanceValid(claw.CurrentTarget) ? claw.CurrentTarget.GetPath().ToString() : "none");
        void Check(bool passed, string condition) => TestAssert.True(passed, "fragment Temple reverse " + condition + "; " + Facts());
        StaticBody2D Support(PrologueSectorId sectorId, string name)
        {
            Check(world.TryGetMountedSector(sectorId, out PrologueSector sector) && Live(sector), "requires the actual mounted support sector");
            return sector.GetNode<StaticBody2D>("Gameplay/Collision/" + name);
        }
        bool Supported(StaticBody2D support)
        {
            if (!Live(support)) return false;
            CollisionShape2D shape = support.GetNode<CollisionShape2D>("Collision");
            if (!TryTask10CapsuleCorridor(player, shape, out _, out _, out _, out _) || !HasFloorContact(player, support)) return false;
            var rectangle = (RectangleShape2D)shape.Shape;
            float top = shape.ToGlobal(new Vector2(0f, -rectangle.Size.Y * .5f)).Y;
            return MathF.Abs(body.GlobalPosition.Y + 36f - top) <= 1f;
        }
        void Safety()
        {
            Check(new Node[] { level, flow, world, player, input, motor, health, claw, body }.All(Live) &&
                level.Player == player && monitor.Player == player && player.GetInstanceId() == playerId &&
                player.GetParent()?.Name == "PlayerRoot" && flow.State == PrologueGameFlowState.Playing &&
                !tree.Paused && sessions() == 1 && hp > 0 && health.CurrentHealth == hp &&
                requests() == 0 && recoveries() == 0 && monitor.RecoveryRequested == 0 && monitor.RecoveryCompleted == 0,
                "keeps the same Playing player, HP, session and zero recovery");
            Check(body.Shape is CapsuleShape2D capsule && capsule.Radius == 20f && capsule.Height == 72f &&
                !body.Disabled && MathF.Abs(body.GlobalTransform.X.Length() - 1f) < .001f &&
                MathF.Abs(body.GlobalTransform.Y.Length() - 1f) < .001f &&
                player.GlobalPosition.IsFinite() && player.Velocity.IsFinite() &&
                motor.AcceptedJumpCount == jumps && motor.AcceptedDashCount == dashes &&
                keyboard.AttackEdges == rawJ && keyboard.ShieldEdges == rawL &&
                !input.CurrentFrame.AttackPressed && !input.CurrentFrame.InteractPressed &&
                !input.CurrentFrame.JumpPressed && !input.CurrentFrame.JumpHeld &&
                !input.CurrentFrame.DashPressed && !input.CurrentFrame.ShieldHeld &&
                claw.CurrentFrame.Failure == MechanicalClawFailure.None && !level.Progress.BossDefeated,
                "retains the real capsule and only the scoped A/D/K route inputs, without physics or Boss mutation");
            if (input.CurrentFrameSequence > lastInput)
            {
                lastInput = input.CurrentFrameSequence;
                if (input.CurrentFrame.KeyboardActions && input.CurrentFrame.MoveAxis < -.9f && Input.IsPhysicalKeyPressed(Key.A)) leftSamples++;
                if (input.CurrentFrame.ClawPressed) { Check(Input.IsPhysicalKeyPressed(Key.K), "samples K while its physical key is still held"); kSamples++; }
            }
        }
        async Task Tick()
        {
            Safety();
            await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
            monitor.Observe(); // Includes KeyboardRouteInputDriver.ObserveFrame; exactly once per sample.
            Safety();
        }
        bool Neutral() => input.CurrentFrame.MoveAxis == 0f && !input.CurrentFrame.ClawPressed &&
            !Input.IsPhysicalKeyPressed(Key.A) && !Input.IsPhysicalKeyPressed(Key.D) && !Input.IsPhysicalKeyPressed(Key.K) &&
            player.Velocity.Length() <= 2f && claw.CurrentFrame.State == MechanicalClawState.Idle;
        void Marker(StaticBody2D support)
        {
            Check(Supported(support) && Neutral(), "publishes only actual complete, neutral named support");
            segments++;
            GD.Print($"QUEST_FRAGMENT_RETURN_SEGMENT_OK name={support.Name} position={player.GlobalPosition} hp={hp} recovery=0");
        }
        async Task WalkDrop(StaticBody2D source, StaticBody2D destination, float targetX, int budget)
        {
            phase = destination.Name;
            Check(Supported(source) && Neutral(), "starts the walk/drop on its previous full named support");
            int samplesBefore = leftSamples, stable = 0;
            bool released = false;
            SendAction(InputActions.MoveLeft, true);
            try
            {
                for (int i = 0; i < budget; i++)
                {
                    float speed = MathF.Max(0f, -player.Velocity.X);
                    float braking = speed * speed / (2f * motor.Tuning.GroundDeceleration);
                    if (!released && player.GlobalPosition.X - targetX <= braking + 4f)
                    { SendAction(InputActions.MoveLeft, false); released = true; }
                    await Tick();
                    stable = released && Supported(destination) && Neutral() ? stable + 1 : 0;
                    if (stable == 12) break;
                }
                Check(stable == 12 && leftSamples > samplesBefore && MathF.Abs(player.GlobalPosition.X - targetX) <= 20f,
                    $"real A walk/drop reaches 12 full-support samples inside its fixed {budget}-frame budget");
                Marker(destination);
            }
            finally { SendAction(InputActions.MoveLeft, false); }
        }

        try
        {
            StaticBody2D checkpoint = Support(PrologueSectorId.LostTemple, "CheckpointFloor");
            int stable = 0;
            for (int i = 0; i < 90 && stable < 12; i++)
            { await Tick(); stable = Supported(checkpoint) && Neutral() ? stable + 1 : 0; }
            Check(stable == 12 && world.CurrentSector == PrologueSectorId.LostTemple &&
                level.Progress.CollectedFragmentIds.Count == 0 && !flow.Save.LegacyCollectionQuotaExempt,
                "initial Continue settles fully at the real Temple checkpoint with zero fragments");
            GD.Print($"QUEST_FRAGMENT_RETURN_FIXTURE_OK schema=4 rule=1 legacy=false fragments=0 player={playerId} hp={hp} checkpoint={flow.Save.CheckpointId}");
            Marker(checkpoint);
            StaticBody2D step = Support(PrologueSectorId.LostTemple, "CourtyardStep");
            StaticBody2D floor = Support(PrologueSectorId.LostTemple, "CourtyardFloor");
            StaticBody2D approach = Support(PrologueSectorId.CanyonAscent, "CourtyardApproach");
            StaticBody2D finalCatch = Support(PrologueSectorId.CanyonAscent, "AscentFinalCatch");
            await WalkDrop(checkpoint, step, 5620f, 100);
            await WalkDrop(step, floor, 5320f, 100);
            await WalkDrop(floor, approach, 5050f, 90);
            await WalkDrop(approach, finalCatch, 4780f, 90);

            phase = "AscentGrip9-K";
            StaticBody2D grip9 = Support(PrologueSectorId.CanyonAscent, "AscentGrip9");
            GrappleSurface2D target = Support(PrologueSectorId.CanyonAscent, "AscentGrip9Grip")
                .GetNode<GrappleSurface2D>("GrappleSurfaceProfile");
            var resolver = new MechanicalClawTargetResolver();
            Check(Neutral() && Supported(finalCatch) && input.KeyboardAimDirection.Dot(Vector2.Left) > .999f,
                "fixed FinalCatch stance retains the actually sampled west keyboard aim");
            MechanicalClawTargetResolution resolved = resolver.ResolveKeyboard(player, input.KeyboardAimDirection);
            Check(resolved.Surface == target && resolved.Snapshot.Exists && resolved.Snapshot.HasLineOfSight &&
                !resolved.Snapshot.IsLegacyInteraction && resolved.Snapshot.RuntimeId == target.GetInstanceId() &&
                resolved.Snapshot.WorldAnchor.IsFinite() && player.GlobalPosition.DistanceTo(resolved.Snapshot.WorldAnchor) <= 620f &&
                target.UseCollisionHitPoint && target.Material == GrappleSurfaceMaterial.TempleMetal,
                "actual west ResolveKeyboard selects the exact original Grip9 post, collision hit point, LOS and range620");
            GD.Print($"QUEST_FRAGMENT_RETURN_K_PREFLIGHT target={target.GetPath()} anchor={resolved.Snapshot.WorldAnchor} distance={player.GlobalPosition.DistanceTo(resolved.Snapshot.WorldAnchor)} direction={input.KeyboardAimDirection} LOS=true");

            int castFrames = 0, sampledBefore = kSamples;
            ulong issue = input.CurrentFrameSequence;
            SendAction(InputActions.Claw, true);
            try
            {
                for (int i = 0; i < 8; i++)
                {
                    await Tick(); castFrames++;
                    if (kSamples == sampledBefore + 1 && claw.CurrentTarget == target &&
                        claw.CurrentFrame.State is MechanicalClawState.Aim or MechanicalClawState.Windup or MechanicalClawState.Travel) break;
                }
                resolved = resolver.ResolveKeyboard(player, input.KeyboardAimDirection);
                Check(input.CurrentFrameSequence > issue && kSamples == sampledBefore + 1 &&
                    keyboard.ClawEdges == rawK + 1 && input.CurrentFrame.KeyboardActions &&
                    input.KeyboardAimDirection.Dot(Vector2.Left) > .999f && resolved.Surface == target &&
                    resolved.Snapshot.HasLineOfSight && player.GlobalPosition.DistanceTo(resolved.Snapshot.WorldAnchor) <= 620f &&
                    claw.CurrentTarget == target &&
                    (claw.CurrentFrame.NextCastId == initialCast || claw.CurrentFrame.NextCastId == initialCast + 1),
                    "one fresh physical west K is admitted to that same real resolver target within8");
            }
            finally { SendAction(InputActions.Claw, false); }
            bool attached = false, arrived = false;
            while (castFrames < 300 && !arrived)
            {
                await Tick(); castFrames++;
                Check(claw.CurrentTarget == target && (claw.CurrentFrame.State == MechanicalClawState.Aim
                    ? claw.CurrentFrame.NextCastId == initialCast && claw.CurrentFrame.CastId == 0
                    : claw.CurrentFrame.NextCastId == initialCast + 1 && claw.CurrentFrame.CastId == initialCast),
                    "keeps exactly the original cast and its original terrain post through natural Aim admission");
                attached |= claw.CurrentFrame.State is MechanicalClawState.Latched or MechanicalClawState.Reel or MechanicalClawState.Swing;
                arrived = attached && claw.CurrentFrame.Arrived && claw.CurrentFrame.State == MechanicalClawState.Swing;
            }
            Check(attached && arrived && claw.CurrentFrame.LockedTargetRuntimeId == target.GetInstanceId(),
                "the one actual post cast reaches attached Swing/Arrived inside the original300 frame budget");
            GD.Print($"QUEST_FRAGMENT_RETURN_K_ARRIVED cast={claw.CurrentFrame.CastId} dock={claw.CurrentFrame.DockPoint} position={player.GlobalPosition} frames={castFrames}");
            issue = input.CurrentFrameSequence;
            SendAction(InputActions.Claw, true);
            try
            {
                for (int i = 0; i < 8 && castFrames < 300; i++)
                {
                    await Tick(); castFrames++;
                    if (kSamples == sampledBefore + 2 && claw.CurrentFrame.ReleaseKind == MechanicalClawReleaseKind.Cancel) break;
                }
                Check(input.CurrentFrameSequence > issue && kSamples == sampledBefore + 2 && keyboard.ClawEdges == rawK + 2 &&
                    claw.CurrentFrame.ReleaseKind == MechanicalClawReleaseKind.Cancel &&
                    claw.CurrentFrame.NextCastId == initialCast + 1, "one fresh second K cancels that attachment without recast, Space or Boost");
            }
            finally { SendAction(InputActions.Claw, false); }
            stable = 0;
            while (castFrames < 300 && stable < 12)
            { await Tick(); castFrames++; stable = Supported(grip9) && Neutral() ? stable + 1 : 0; }
            Check(stable == 12, "neutral gravity after real K cancellation lands the full capsule on Grip9 within the same300 frame budget");
            Marker(grip9);
            StaticBody2D split = Support(PrologueSectorId.CanyonAscent, "SplitUpperGallery");
            await WalkDrop(grip9, split, 4220f, 90);
            Check(segments == 7 && kSamples == 2 && keyboard.ClawEdges == rawK + 2 && leftSamples > 0 &&
                motor.AcceptedJumpCount == jumps && motor.AcceptedDashCount == dashes &&
                claw.CurrentFrame.NextCastId == initialCast + 1, "all seven supported segments retain genuine scoped key witnesses");
            monitor.AssertNoProductionRecovery("fragment Temple reverse region fixture");
            GD.Print($"QUEST_FRAGMENT_RETURN_KEYS A_samples={leftSamples} K_samples={kSamples} K_edges={keyboard.ClawEdges - rawK} J_edges={keyboard.AttackEdges - rawJ} L_edges={keyboard.ShieldEdges - rawL} jumps={motor.AcceptedJumpCount - jumps} dashes={motor.AcceptedDashCount - dashes} observed_frames={keyboard.ObservedFrames} fragments={level.Progress.CollectedFragmentIds.Count} remaining=Split-to-Camp-and-Abyss fullRoute=false");
        }
        catch (Exception error)
        {
            GD.Print($"QUEST_FRAGMENT_RETURN_FAILURE type={error.GetType().Name} {Facts()}");
            throw;
        }
        finally { keyboard.ReleaseAll(); }
    }
}
