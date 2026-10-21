using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Knight.Abilities;
using Knight.Combat;
using Knight.Player;
using Knight.Prologue;
using Knight.Prologue.Seamless;
using Knight.Prologue.Traversal;
using Knight.Save;
using Knight.Shared;
using Knight.Tests.Presentation;
using Knight.World;

namespace Knight.Tests.Integration;

public static partial class PrologueCanyonGeneratedMainRouteTests
{
    // A narrow native characterization, not a natural zero-to-six return route.
    // Continue is the only placement operation; every subsequent displacement is
    // the original motor/claw consuming real physical key events.
    private static async Task DriveFragmentBasinAbyssReturn(SceneTree tree, PrologueLevelController level,
        PrologueGameFlowController flow, SeamlessPrologueWorld world, CharacterBody2D player,
        RouteMonitor monitor, KeyboardRouteInputDriver keyboard, int hp,
        Func<int> sessions, Func<int> requests, Func<int> recoveries, bool chained = false)
    {
        PlayerInputReader input = player.GetNode<PlayerInputReader>("Input");
        PlayerMotor motor = player.GetNode<PlayerMotor>("Motor");
        HealthComponent health = player.GetNode<HealthComponent>("Health");
        MechanicalClawController claw = player.GetNode<MechanicalClawController>("MechanicalClaw");
        CollisionShape2D body = player.GetNode<CollisionShape2D>("BodyCollider");
        ulong playerId = player.GetInstanceId(), initialCast = claw.CurrentFrame.NextCastId;
        int jumps = motor.AcceptedJumpCount, dashes = motor.AcceptedDashCount;
        int rawK = keyboard.ClawEdges, rawJ = keyboard.AttackEdges, rawL = keyboard.ShieldEdges;
        string checkpointId = chained ? "canyon.v2.temple-forecourt" : "canyon.v2.basin-camp";
        string[] initialIds = chained ? new[] { "canyon.v2.fragment.courtyard" } : Array.Empty<string>();
        string[] expectedIds = initialIds.Append("canyon.v2.fragment.abyss-floor").OrderBy(id => id, StringComparer.Ordinal).ToArray();
        string[] mechanisms = chained ? new[] { CanyonWorldLayout.CourtyardSealMechanismId } : Array.Empty<string>();
        string[] shortcuts = chained ? new[] { CanyonWorldLayout.UpperBasinShortcutId, "canyon.v2.temple-bell-gate" } : Array.Empty<string>();
        bool IdsEqual(System.Collections.Generic.IEnumerable<string> actual, string[] expected) =>
            actual.OrderBy(id => id, StringComparer.Ordinal).SequenceEqual(expected.OrderBy(id => id, StringComparer.Ordinal));
        int kSamples = 0, wSamples = 0, segments = 0;
        ulong lastInput = input.CurrentFrameSequence;
        string phase = "camp-settle";
        bool Live(Node node) => node != null && GodotObject.IsInstanceValid(node) && node.IsInsideTree() && !node.IsQueuedForDeletion();
        string Facts() => $"phase={phase} physics={Engine.GetPhysicsFrames()} player={player.GlobalPosition} " +
            $"velocity={player.Velocity} floor={player.IsOnFloor()} hp={health.CurrentHealth}/{hp} " +
            $"recovery={requests()}/{recoveries()} aim={input.KeyboardAimDirection} " +
            $"claw={claw.CurrentFrame.State}/{claw.CurrentFrame.Failure} target=" +
            (GodotObject.IsInstanceValid(claw.CurrentTarget) ? claw.CurrentTarget.GetPath().ToString() : "none");
        void Check(bool value, string condition) => TestAssert.True(value, "fragment Basin/Abyss " + condition + "; " + Facts());
        StaticBody2D Support(PrologueSectorId id, string name)
        {
            Check(world.TryGetMountedSector(id, out PrologueSector sector) && Live(sector), "requires its actual mounted sector");
            return sector.GetNode<StaticBody2D>("Gameplay/Collision/" + name);
        }
        bool Supported(StaticBody2D support)
        {
            CollisionShape2D shape = support.GetNode<CollisionShape2D>("Collision");
            if (!TryTask10CapsuleCorridor(player, shape, out _, out _, out _, out _) || !HasFloorContact(player, support)) return false;
            float top = shape.ToGlobal(new Vector2(0, -((RectangleShape2D)shape.Shape).Size.Y * .5f)).Y;
            return MathF.Abs(body.GlobalPosition.Y + 36f - top) <= 1f;
        }
        void Safety()
        {
            Check(new Node[] { level, flow, world, player, input, motor, health, claw, body }.All(Live) &&
                level.Player == player && monitor.Player == player && player.GetInstanceId() == playerId &&
                flow.State == PrologueGameFlowState.Playing && !tree.Paused && sessions() == 1 &&
                health.CurrentHealth == hp && hp > 0 && requests() == 0 && recoveries() == 0 &&
                monitor.RecoveryRequested == 0 && monitor.RecoveryCompleted == 0,
                "retains the same alive player, full initial HP and one session, with no recovery");
            Check(body.Shape is CapsuleShape2D capsule && capsule.Radius == 20 && capsule.Height == 72 && !body.Disabled &&
                MathF.Abs(body.GlobalTransform.X.Length() - 1) < .001f && MathF.Abs(body.GlobalTransform.Y.Length() - 1) < .001f &&
                player.GlobalPosition.IsFinite() && player.Velocity.IsFinite() &&
                motor.AcceptedJumpCount == jumps && motor.AcceptedDashCount == dashes &&
                keyboard.AttackEdges == rawJ && keyboard.ShieldEdges == rawL &&
                !input.CurrentFrame.AttackPressed && !input.CurrentFrame.InteractPressed &&
                !input.CurrentFrame.JumpPressed && !input.CurrentFrame.JumpHeld && !input.CurrentFrame.DashPressed &&
                !input.CurrentFrame.ShieldHeld && claw.CurrentFrame.Failure == MechanicalClawFailure.None &&
                !level.Progress.BossDefeated && !flow.Save.LegacyCollectionQuotaExempt,
                "uses the unchanged real capsule and only scoped A/D/W/K, never jump/dash/healing/exemption");
            if (input.CurrentFrameSequence > lastInput)
            {
                lastInput = input.CurrentFrameSequence;
                if (Input.IsPhysicalKeyPressed(Key.W) && input.KeyboardAimDirection.Dot(Vector2.Up) > .999f) wSamples++;
                if (input.CurrentFrame.ClawPressed) { Check(Input.IsPhysicalKeyPressed(Key.K), "samples a still-held physical K"); kSamples++; }
            }
        }
        async Task Tick()
        {
            Safety();
            await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
            monitor.Observe();
            Safety();
        }
        bool Neutral() => input.CurrentFrame.MoveAxis == 0 && !input.CurrentFrame.ClawPressed &&
            !Input.IsPhysicalKeyPressed(Key.A) && !Input.IsPhysicalKeyPressed(Key.D) &&
            !Input.IsPhysicalKeyPressed(Key.W) && !Input.IsPhysicalKeyPressed(Key.K) &&
            player.Velocity.Length() <= 2 && claw.CurrentFrame.State == MechanicalClawState.Idle;
        void Marker(StaticBody2D support)
        {
            Check(Supported(support) && Neutral(), "requires neutral full capsule support for a segment marker");
            GD.Print($"QUEST_FRAGMENT_RETURN_SEGMENT_OK name={support.Name} position={player.GlobalPosition} hp={hp} recovery=0");
            segments++;
        }
        async Task WalkTo(StaticBody2D destination, float x, int budget)
        {
            bool left = player.GlobalPosition.X > x, released = false;
            StringName action = left ? InputActions.MoveLeft : InputActions.MoveRight;
            phase = destination.Name + "-walk";
            int stable = 0;
            SendAction(action, true);
            try
            {
                for (int i = 0; i < budget; i++)
                {
                    float speed = MathF.Abs(player.Velocity.X);
                    float braking = speed * speed / (2 * motor.Tuning.GroundDeceleration);
                    float remaining = left ? player.GlobalPosition.X - x : x - player.GlobalPosition.X;
                    if (!released && remaining <= braking + 4) { SendAction(action, false); released = true; }
                    await Tick();
                    stable = released && Supported(destination) && Neutral() ? stable + 1 : 0;
                    if (stable == 12) break;
                }
                Check(stable == 12 && MathF.Abs(player.GlobalPosition.X - x) <= 20,
                    $"reaches the named destination with12 neutral full-support samples inside{budget}");
            }
            finally { SendAction(action, false); }
        }
        try
        {
            StaticBody2D camp = Support(PrologueSectorId.CanyonBasin, "BasinCampFloor");
            StaticBody2D bottom = Support(PrologueSectorId.CanyonDescent, "AbyssSafeBottom");
            Check(world.TryGetMountedSector(PrologueSectorId.CanyonDescent, out PrologueSector descent), "requires live Descent recovery geometry");
            Check(world.TryGetMountedSector(PrologueSectorId.CanyonBasin, out PrologueSector basin), "requires the original live Basin source sector");
            TraversalSegment2D returnContract = basin.GetNode<TraversalSegment2D>(
                "Gameplay/TraversalContracts/canyonv2routebasin-camp-to-abyss");
            Check(returnContract.Kind == TraversalSegmentKind.ClawDrop &&
                returnContract.RequiredAbilities == TraversalAbility.Thread &&
                returnContract.Validate(basin).Count == 0,
                "saved cross-sector return contract resolves the actual claw face and landing support, without Jump or Dash");
            KillZone[] zones = descent.GetNode("Gameplay/FallRecovery").GetChildren().OfType<KillZone>()
                .OrderBy(zone => zone.GlobalPosition.X).ToArray();
            Check(zones.Length == 2, "keeps two actual fall recovery areas outside the bounded return corridor");
            Rect2[] expected = { new(1280, 3584, 336, 16), new(1996, 3584, 564, 16) };
            for (int i = 0; i < zones.Length; i++)
            {
                CollisionShape2D shape = zones[i].GetNode<CollisionShape2D>("Collision");
                Check(Live(zones[i]) && zones[i].Monitoring && !shape.Disabled &&
                    shape.Shape is RectangleShape2D rectangle && rectangle.Size == expected[i].Size &&
                    shape.ToGlobal(-rectangle.Size * .5f) == expected[i].Position,
                    "saved/live recovery still covers the exact independent left/right non-passage spans");
            }
            GD.Print("QUEST_FRAGMENT_BASIN_RECOVERY_GEOMETRY_OK world_open=1616..1996 left_covered=1280..1616 right_covered=1996..2560");
            int stable = 0;
            for (int i = 0; i < 90 && stable < 12; i++)
            { await Tick(); stable = Supported(camp) && Neutral() ? stable + 1 : 0; }
            Check(stable == 12 && IdsEqual(level.Progress.CollectedFragmentIds, initialIds) &&
                IdsEqual(flow.Save.CollectedFragmentIds, initialIds) && flow.Save.CheckpointId == checkpointId &&
                IdsEqual(flow.Save.ActivatedMechanisms, mechanisms) && IdsEqual(flow.Save.OpenShortcuts, shortcuts),
                "entry retains its actual upstream fragments/checkpoint/prerequisites on the full real camp support");
            Marker(camp);
            await WalkTo(camp, 1720, 90);
            phase = "brake-west-face-preflight";
            GrappleSurface2D target = Support(PrologueSectorId.CanyonDescent, "AbyssBrakeLeft4")
                .GetNode<GrappleSurface2D>("GrappleSurfaceProfile");
            SendAction(InputActions.MoveUp, true);
            for (int i = 0; i < 3; i++) await Tick();
            var resolver = new MechanicalClawTargetResolver();
            MechanicalClawTargetResolution resolved = resolver.ResolveKeyboard(player, input.KeyboardAimDirection);
            GD.Print($"QUEST_FRAGMENT_BASIN_K_PREFLIGHT requested={target.GetPath()} selected=" +
                (GodotObject.IsInstanceValid(resolved.Surface) ? resolved.Surface.GetPath().ToString() : "none") +
                $" anchor={resolved.Snapshot.WorldAnchor} normal={resolved.Snapshot.SurfaceNormal} distance={player.GlobalPosition.DistanceTo(resolved.Snapshot.WorldAnchor)} LOS={resolved.Snapshot.HasLineOfSight} position={player.GlobalPosition} aim={input.KeyboardAimDirection}");
            Check(Supported(camp) && resolved.Surface == target && resolved.Snapshot.HasLineOfSight &&
                resolved.Snapshot.RuntimeId == target.GetInstanceId() && !resolved.Snapshot.IsLegacyInteraction &&
                target.UseCollisionHitPoint && resolved.Snapshot.SurfaceNormal.Dot(Vector2.Left) > .999f &&
                player.GlobalPosition.DistanceTo(resolved.Snapshot.WorldAnchor) <= 620,
                "actual W selects the original brake west face, native normal, clear LOS and unchanged range620");
            Vector2 contractHit = target.GetParent().GetNode<CollisionShape2D>("Collision")
                .ToGlobal(returnContract.AssistHitPoint);
            Check(returnContract.LaunchZone.HasPoint(returnContract.ToLocal(player.GlobalPosition)) &&
                contractHit.DistanceTo(resolved.Snapshot.WorldAnchor) < .01f &&
                returnContract.AssistHitNormal == resolved.Snapshot.SurfaceNormal,
                "real keyboard preview consumes the authored launch/actual face point and normal, not a wall-center estimate");
            SendAction(InputActions.Claw, true);
            for (int i = 0; i < 8 && kSamples == 0; i++) await Tick();
            Check(kSamples == 1 && keyboard.ClawEdges == rawK + 1 && claw.CurrentTarget == target,
                "one real W+K is consumed for the exact original terrain target");
            SendAction(InputActions.Claw, false);
            int castFrames = 0;
            bool attached = false, arrived = false;
            for (; castFrames < 300 && !arrived; castFrames++)
            {
                await Tick();
                Check(claw.CurrentTarget == target && claw.CurrentFrame.NextCastId <= initialCast + 1,
                    "retains one target/cast throughout native admission and travel");
                attached |= claw.CurrentFrame.State is MechanicalClawState.Latched or MechanicalClawState.Reel or MechanicalClawState.Swing;
                arrived = attached && claw.CurrentFrame.State == MechanicalClawState.Swing && claw.CurrentFrame.Arrived;
            }
            Check(arrived && claw.CurrentFrame.LockedTargetRuntimeId == target.GetInstanceId() &&
                claw.CurrentFrame.CastId == initialCast && claw.CurrentFrame.NextCastId == initialCast + 1,
                "the only cast naturally reaches the original west face inside300");
            GD.Print($"QUEST_FRAGMENT_BASIN_K_ARRIVED cast={claw.CurrentFrame.CastId} anchor={claw.CurrentFrame.AnchorPoint} dock={claw.CurrentFrame.DockPoint} position={player.GlobalPosition} frames={castFrames}");
            SendAction(InputActions.Claw, true);
            for (int i = 0; i < 8 && kSamples < 2; i++) await Tick();
            Check(kSamples == 2 && claw.CurrentFrame.ReleaseKind == MechanicalClawReleaseKind.Cancel &&
                claw.CurrentFrame.NextCastId == initialCast + 1, "fresh K cancels without Boost, ordinary jump or recast");
            SendAction(InputActions.Claw, false);
            SendAction(InputActions.MoveUp, false);
            // The measured west-face dock is above the open lower end of the
            // original brake wall. Natural gravity must carry the whole capsule
            // around that end before D can reach the original padded bottom.
            // No helper platform, collision exemption or player pose is authored.
            await WalkTo(bottom, 2240, 180);
            Marker(bottom);
            Check(returnContract.LandingZone.HasPoint(returnContract.ToLocal(player.GlobalPosition)),
                "real post-claw gravity and movement reach the declared original bottom, not an intermediate dock");
            GD.Print($"QUEST_FRAGMENT_BASIN_CONTRACT_OK id={returnContract.SegmentId} kind=ClawDrop abilities=Thread hit={contractHit} normal={returnContract.AssistHitNormal} final_support=AbyssSafeBottom physicalRoute=regional_fixture");
            const string id = "canyon.v2.fragment.abyss-floor";
            Check(IdsEqual(level.Progress.CollectedFragmentIds, expectedIds) &&
                IdsEqual(flow.Save.CollectedFragmentIds, expectedIds) && expectedIds.Contains(id, StringComparer.Ordinal),
                "actual bottom contact adds exactly its stable fragment ID without losing any upstream collection");
            await WalkTo(camp, 1820, 180);
            Marker(camp);
            PrologueSaveData saved = PrologueSaveStore.LoadOrNew(flow.SavePath, 2);
            Check(!saved.WasRecovered && IdsEqual(saved.CollectedFragmentIds, expectedIds) &&
                saved.CheckpointId == checkpointId && !saved.LegacyCollectionQuotaExempt &&
                IdsEqual(saved.ActivatedMechanisms, mechanisms) && IdsEqual(saved.OpenShortcuts, shortcuts) &&
                !saved.BossDefeated && !saved.Completed && segments == 3 && kSamples == 2 && wSamples > 0 &&
                keyboard.ClawEdges == rawK + 2 && motor.AcceptedJumpCount == jumps && motor.AcceptedDashCount == dashes,
                "back at camp, isolated durable state retains exactly the actual fragment delta and its scoped upstream state, never completion");
            monitor.AssertNoProductionRecovery("Basin/Abyss collection return fixture");
            GD.Print($"QUEST_FRAGMENT_BASIN_RETURN_KEYS W_samples={wSamples} K_samples={kSamples} K_edges={keyboard.ClawEdges - rawK} J_edges=0 L_edges=0 jumps=0 dashes=0 fragments={expectedIds.Length} fullRoute=false");
        }
        catch (Exception error)
        {
            GD.Print($"QUEST_FRAGMENT_BASIN_RETURN_FAILURE type={error.GetType().Name} {Facts()}");
            throw;
        }
        finally { keyboard.ReleaseAll(); }
    }
}
