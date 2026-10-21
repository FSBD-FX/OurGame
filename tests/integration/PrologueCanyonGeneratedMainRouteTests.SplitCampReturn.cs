using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using Knight.Abilities;
using Knight.Combat;
using Knight.Mechanisms;
using Knight.Player;
using Knight.Prologue;
using Knight.Prologue.Seamless;
using Knight.Prologue.Traversal;
using Knight.Shared;
using Knight.Tests.Presentation;

namespace Knight.Tests.Integration;

public static partial class PrologueCanyonGeneratedMainRouteTests
{
    // Continues the actual upstream Temple player. It neither mounts a second
    // fixture nor seeds/reloads/places the player between the regional drivers.
    private static async Task DriveFragmentSplitToCamp(SceneTree tree, PrologueLevelController level,
        PrologueGameFlowController flow, SeamlessPrologueWorld world, CharacterBody2D player,
        RouteMonitor monitor, KeyboardRouteInputDriver keyboard, int hp,
        Func<int> sessions, Func<int> requests, Func<int> recoveries)
    {
        PlayerInputReader input = player.GetNode<PlayerInputReader>("Input");
        PlayerMotor motor = player.GetNode<PlayerMotor>("Motor");
        HealthComponent health = player.GetNode<HealthComponent>("Health");
        MechanicalClawController claw = player.GetNode<MechanicalClawController>("MechanicalClaw");
        CollisionShape2D body = player.GetNode<CollisionShape2D>("BodyCollider");
        ulong playerId = player.GetInstanceId(), nextCast = claw.CurrentFrame.NextCastId;
        int jumps = motor.AcceptedJumpCount, dashes = motor.AcceptedDashCount;
        int rawK = keyboard.ClawEdges, rawJ = keyboard.AttackEdges, rawL = keyboard.ShieldEdges;
        int guardEdges = 0, guardedPulses = 0, lanePulsesBefore = 0, guardResults = 0, otherGuards = 0;
        var seenGuardIdentities = new HashSet<DamageAttackIdentity>();
        bool defensePhase = false, rightGuardValid = true;
        PlayerShieldController shield = player.GetNode<PlayerShieldController>("Shield");
        PlayerEnergyController energy = player.GetNode<PlayerEnergyController>("Energy");
        int segments = 0;
        string phase = "SplitUpperGallery-entry";
        bool Live(Node node) => node != null && GodotObject.IsInstanceValid(node) && node.IsInsideTree() && !node.IsQueuedForDeletion();
        TestAssert.True(world.TryGetMountedSector(PrologueSectorId.CanyonAscent, out PrologueSector ascent),
            "same-player Split return requires the original mounted Ascent");
        TestAssert.True(world.TryGetMountedSector(PrologueSectorId.CanyonBasin, out PrologueSector basin),
            "same-player Split return requires the original mounted Basin");
        CanyonShortcutWinch winch = ascent.GetNode<CanyonShortcutWinch>("Gameplay/Mechanisms/UpperBasinShortcutWinch");
        CanyonSteamDiverter steam = basin.GetNode<CanyonSteamDiverter>("Gameplay/Mechanisms/SteamDiverterTrench");
        CanyonSteamJet right = steam.GetNode<CanyonSteamJet>("RightJet");
        CanyonSteamJet lowerJet = basin.GetNode<CanyonSteamJet>("Gameplay/Mechanisms/SteamJet");
        StaticBody2D entrySupport = basin.GetNode<StaticBody2D>("Gameplay/Collision/GearEntry");
        StaticBody2D pistonSupport = basin.GetNode<StaticBody2D>("Gameplay/Collision/GearPistonOne");
        CollisionShape2D entryCollision = entrySupport.GetNode<CollisionShape2D>("Collision");
        CollisionShape2D pistonCollision = pistonSupport.GetNode<CollisionShape2D>("Collision");
        Hurtbox hurt = player.GetNode<Hurtbox>("Hurtbox");
        PlayerGuardedDamageReceiver receiver = player.GetNode<PlayerGuardedDamageReceiver>("GuardedDamage");
        CanyonShieldLaneHazard lane = basin.GetNode<CanyonShieldLaneHazard>("Gameplay/ShieldLane");
        // Read only the actual receiver's latest bounded attack identity, not a
        // memory dump or a synthetic source inferred from knockback parameters.
        FieldInfo attacksField = typeof(PlayerGuardedDamageReceiver).GetField("_processedAttackOrder",
            BindingFlags.NonPublic | BindingFlags.Instance);
        CheckIdentityField();
        void CheckIdentityField() => TestAssert.True(attacksField != null,
            "fragment return diagnostic requires the original receiver's bounded identity queue");
        string Facts() => $"phase={phase} physics={Engine.GetPhysicsFrames()} position={player.GlobalPosition} " +
            $"velocity={player.Velocity} floor={player.IsOnFloor()} hp={health.CurrentHealth}/{hp} recovery={requests()}/{recoveries()} " +
            $"jump={motor.AcceptedJumpCount - jumps} claw={claw.CurrentFrame.State}/{claw.CurrentFrame.Failure} " +
            $"steam={steam.SelectedSide}/{steam.HasActivated}/{steam.IsHandingOff} right={right.Frame.HazardActive} " +
            $"guards={guardedPulses}/{lane.PulseCount - lanePulsesBefore} otherGuards={otherGuards} results={guardResults} guardValid={rightGuardValid} L={keyboard.ShieldEdges - rawL}/{guardEdges} " +
            $"shield={shield.IsActive}/{shield.GuardDirection} energy={energy.EnergyRatio}/{energy.HeatRatio}/{energy.IsOverheated}";
        void Check(bool valid, string condition) => TestAssert.True(valid, "fragment Split/Camp " + condition + "; " + Facts());
        bool Supported(StaticBody2D support)
        {
            CollisionShape2D shape = support.GetNode<CollisionShape2D>("Collision");
            if (!TryTask10CapsuleCorridor(player, shape, out _, out _, out _, out _) || !HasFloorContact(player, support)) return false;
            float top = shape.ToGlobal(new Vector2(0, -((RectangleShape2D)shape.Shape).Size.Y * .5f)).Y;
            return MathF.Abs(body.GlobalPosition.Y + 36 - top) <= 1;
        }
        bool Neutral() => input.CurrentFrame.MoveAxis == 0 && !input.CurrentFrame.JumpHeld && !input.CurrentFrame.JumpPressed &&
            !Input.IsPhysicalKeyPressed(Key.A) && !Input.IsPhysicalKeyPressed(Key.D) && !Input.IsPhysicalKeyPressed(Key.Space) &&
            !input.CurrentFrame.ShieldHeld && !Input.IsPhysicalKeyPressed(Key.L) &&
            player.Velocity.Length() <= 2 && claw.CurrentFrame.State == MechanicalClawState.Idle;
        void Safety()
        {
            if (defensePhase)
            {
                // ShieldResolved runs before the receiver remembers the current
                // identity. Reconcile after the completed native tick, so an
                // actual enemy guard is not miscounted as a lane pulse.
                foreach (DamageAttackIdentity identity in (Queue<DamageAttackIdentity>)attacksField.GetValue(receiver))
                {
                    if (!seenGuardIdentities.Add(identity)) continue;
                    bool isLane = identity.SourceRuntimeId == lane.GetInstanceId();
                    if (isLane) guardedPulses++; else otherGuards++;
                    GD.Print($"QUEST_FRAGMENT_RETURN_GUARD_IDENTITY physics={Engine.GetPhysicsFrames()} source={identity.SourceRuntimeId}/{identity.LocalAttackId} lane={isLane} path={(GodotObject.InstanceFromId(identity.SourceRuntimeId) as Node)?.GetPath()}");
                }
            }
            Check(new Node[] { level, flow, world, player, input, motor, health, claw, body, ascent, basin, winch, steam, right, lowerJet, hurt, receiver, lane, shield, energy }.All(Live) &&
                level.Player == player && monitor.Player == player && player.GetInstanceId() == playerId &&
                flow.State == PrologueGameFlowState.Playing && !tree.Paused && sessions() == 1 &&
                health.CurrentHealth == hp && requests() == 0 && recoveries() == 0 &&
                monitor.RecoveryRequested == 0 && monitor.RecoveryCompleted == 0,
                "retains the original alive Playing player/session/HP, never recovery");
            Check(body.Shape is CapsuleShape2D capsule && capsule.Radius == 20 && capsule.Height == 72 && !body.Disabled &&
                MathF.Abs(body.GlobalTransform.X.Length() - 1) < .001f && MathF.Abs(body.GlobalTransform.Y.Length() - 1) < .001f &&
                player.GlobalPosition.IsFinite() && player.Velocity.IsFinite() &&
                motor.AcceptedDashCount == dashes && motor.AcceptedJumpCount >= jumps && motor.AcceptedJumpCount <= jumps + 3 &&
                claw.CurrentFrame.NextCastId == nextCast && claw.CurrentFrame.Failure == MechanicalClawFailure.None &&
                keyboard.ClawEdges == rawK && keyboard.AttackEdges == rawJ && keyboard.ShieldEdges == rawL + guardEdges &&
                !input.CurrentFrame.ClawPressed && !input.CurrentFrame.AttackPressed && !input.CurrentFrame.InteractPressed &&
                (defensePhase || !input.CurrentFrame.ShieldHeld) && !input.CurrentFrame.DashPressed &&
                rightGuardValid && !energy.IsOverheated && (!defensePhase ||
                    guardedPulses == lane.PulseCount - lanePulsesBefore && guardedPulses + otherGuards == guardResults),
                "uses only A/D/at most three original single jumps and disclosed real right-facing L guards, without resizing/attack/cast/dash or missed pulses");
            Check(winch.IsOpen && winch.BarrierCollision.Disabled && !steam.HasActivated &&
                steam.SelectedSide == CanyonSteamDiverterSide.Right && !steam.IsHandingOff &&
                !lowerJet.DisabledForRun &&
                flow.Save.CheckpointId == "canyon.v2.temple-forecourt" && !flow.Save.LegacyCollectionQuotaExempt &&
                level.Progress.CollectedFragmentIds.SequenceEqual(new[] { "canyon.v2.fragment.courtyard" }) &&
                flow.Save.CollectedFragmentIds.SequenceEqual(new[] { "canyon.v2.fragment.courtyard" }) &&
                !level.Progress.BossDefeated,
                "retains upstream real courtyard collection/Temple checkpoint and disclosed open shortcut, without operating the default Right steam fixture");
        }
        void Observe() { monitor.Observe(); Safety(); }
        async Task Tick() { Safety(); await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame); Observe(); }
        async Task SettleAndMark(StaticBody2D support)
        {
            keyboard.ReleaseAll();
            int stable = 0;
            for (int i = 0; i < 60 && stable < 12; i++)
            { await Tick(); stable = Supported(support) && Neutral() ? stable + 1 : 0; }
            Check(stable == 12, "each original named support supplies12 neutral complete-capsule samples within60");
            segments++;
            GD.Print($"QUEST_FRAGMENT_RETURN_SEGMENT_OK name={support.Name} position={player.GlobalPosition} hp={hp} recovery=0");
        }
        async Task Reach(PrologueSector sector, string name)
        {
            phase = name;
            StaticBody2D support = sector.GetNode<StaticBody2D>("Gameplay/Collision/" + name);
            await PrologueCanyonTraversalTests.Task4ReachSupport(tree, player, support, false);
            await SettleAndMark(support);
        }
        async Task HopCampReturnStep()
        {
            // A generic 90px stand-off launches beneath the piston ceiling and
            // loses the hop before reaching this low step. Walk close using the
            // real floor first, then use one physical Space while A is moving.
            phase = "ShortcutCampReturnStep-close-hop";
            StaticBody2D step = basin.GetNode<StaticBody2D>("Gameplay/Collision/ShortcutCampReturnStep");
            int before = motor.AcceptedJumpCount;
            Check(Supported(basin.GetNode<StaticBody2D>("Gameplay/Collision/ShortcutBasinWalkway")) && Neutral(),
                "low-ceiling hop starts on its actual previous support");
            SendAction(InputActions.MoveLeft, true);
            int approach = 0;
            while (approach < 30 && player.GlobalPosition.X > 2489)
            { await Tick(); approach++; }
            Check(approach < 30 && player.IsOnFloor() && player.GlobalPosition.X is > 2480 and <= 2489 &&
                player.Velocity.X < -250 && motor.AcceptedJumpCount == before,
                "closes to the real edge with ordinary ground velocity before its one jump");
            SendAction(InputActions.Jump, true);
            bool landed = false, released = false;
            for (int i = 0; i < 90 && !landed; i++)
            {
                await Tick();
                if (i < 10 || i % 15 == 0)
                    GD.Print($"QUEST_FRAGMENT_RETURN_LOW_HOP physics={Engine.GetPhysicsFrames()} frame={i} position={player.GlobalPosition} velocity={player.Velocity} floor={player.IsOnFloor()} jumps={motor.AcceptedJumpCount - before}");
                if (!released && player.GlobalPosition.X <= 2430)
                { SendAction(InputActions.MoveLeft, false); released = true; }
                landed = released && Supported(step) && player.Velocity.Length() <= 2;
            }
            keyboard.ReleaseAll();
            Check(landed && motor.AcceptedJumpCount == before + 1,
                "one close physical single jump reaches the original low step within90 without a second jump or altered ceiling");
            await SettleAndMark(step);
        }
        async Task HopGearEntry()
        {
            phase = "GearEntry-guarded-return";
            StaticBody2D step = basin.GetNode<StaticBody2D>("Gameplay/Collision/ShortcutCampReturnStep");
            StaticBody2D entry = basin.GetNode<StaticBody2D>("Gameplay/Collision/GearEntry");
            int before = motor.AcceptedJumpCount;
            Check(Supported(step) && Neutral(), "final single hop begins on the original fully supported return step");
            SendAction(InputActions.MoveLeft, true);
            for (int i = 0; i < 30 && player.GlobalPosition.X > 2400.2f; i++) await Tick();
            keyboard.ReleaseAll();
            for (int i = 0; i < 12; i++) await Tick();
            Check(Supported(step) && Neutral() && player.GlobalPosition.X is > 2400 and <= 2400.2f,
                "stages at the original GearEntry wall, never relocates into the return gap");
            lanePulsesBefore = lane.PulseCount;
            seenGuardIdentities.UnionWith((Queue<DamageAttackIdentity>)attacksField.GetValue(receiver));
            defensePhase = true;
            void Guard(bool active)
            {
                int edges = keyboard.ShieldEdges;
                SendAction(InputActions.KeyboardShield, active);
                if (active) guardEdges++;
                Check(keyboard.ShieldEdges == edges + (active ? 1 : 0), "defense uses exactly the disclosed physical L edges");
            }
            async Task FaceRightAndGuard()
            {
                SendAction(InputActions.MoveLeft, false);
                SendAction(InputActions.MoveRight, true);
                Guard(true);
                await Tick();
                SendAction(InputActions.MoveRight, false);
                await Tick();
                Check(shield.IsActive && shield.GuardDirection.Dot(Vector2.Right) > .99f,
                    "actual sampled D/L raises a right guard before the next lane pulse");
            }
            await FaceRightAndGuard();
            SendAction(InputActions.Jump, true);
            int frames = 0;
            while (frames++ < 30 && lane.PulseCount == lanePulsesBefore) await Tick();
            Check(frames <= 30 && motor.AcceptedJumpCount == before + 1 && guardedPulses > 0,
                "the one real jump enters the unchanged lane behind its first right guard");
            while (frames < 300 && (player.GlobalPosition.X > 2110 || !Supported(entry) || lane.GetOverlappingBodies().Contains(player)))
            {
                int pulse = lane.PulseCount;
                Guard(false);
                SendAction(InputActions.MoveLeft, true);
                // The old2125 brake stopped at2111, still inside the lane, then
                // skipped every left burst and only nudged right until overheat.
                // Seven more px lets natural braking clear the2130 lane edge
                // while keeping the complete capsule inside GearEntry (>=2100).
                for (int i = 0; i < 9 && player.GlobalPosition.X > 2118; i++) { await Tick(); frames++; }
                await FaceRightAndGuard(); frames += 2;
                for (int i = 0; i < 20 && frames < 300; i++)
                {
                    await Tick(); frames++;
                    if (lane.PulseCount > pulse || player.GlobalPosition.X <= 2110 && Supported(entry) &&
                        !lane.GetOverlappingBodies().Contains(player)) break;
                }
                GD.Print($"QUEST_FRAGMENT_RETURN_GEAR_GUARD physics={Engine.GetPhysicsFrames()} frames={frames} position={player.GlobalPosition} velocity={player.Velocity} floor={player.IsOnFloor()} jumps={motor.AcceptedJumpCount - before} guards={guardedPulses} pulses={lane.PulseCount - lanePulsesBefore} L_edges={guardEdges}");
            }
            Guard(false);
            keyboard.ReleaseAll();
            Check(frames < 300 && Supported(entry) && player.GlobalPosition.X <= 2110 && !lane.GetOverlappingBodies().Contains(player) &&
                motor.AcceptedJumpCount == before + 1 && guardedPulses > 0 && guardedPulses == lane.PulseCount - lanePulsesBefore,
                "one physical single jump plus real timed right guards clears the60px return channel and unchanged pulse lane within300, without damage or an extra jump");
            await SettleAndMark(entry);
            defensePhase = false;
        }
        void Guarded(PlayerShieldResult result, ulong transition)
        {
            if (!defensePhase) return;
            rightGuardValid &= result.Guarded && result.DamageAmount == 0 && shield.GuardDirection.Dot(Vector2.Right) > .99f;
            guardResults++;
        }
        void Damaged(int remaining, Vector2 impulse)
        {
            var attacks = (Queue<DamageAttackIdentity>)attacksField.GetValue(receiver);
            DamageAttackIdentity identity = attacks.LastOrDefault();
            string source = (GodotObject.InstanceFromId(identity.SourceRuntimeId) as Node)?.GetPath().ToString() ?? "none";
            GD.Print($"QUEST_FRAGMENT_SPLIT_CAMP_DAMAGE physics={Engine.GetPhysicsFrames()} position={player.GlobalPosition} " +
                $"remaining={remaining} impulse={impulse} lowerJet={lowerJet.GlobalPosition}/{lowerJet.Frame.HazardActive}/{lowerJet.Monitoring} " +
                $"source={identity.SourceRuntimeId}/{identity.LocalAttackId}/{source} laneId={lane.GetInstanceId()} laneOverlap={lane.GetOverlappingBodies().Contains(player)} lanePulses={lane.PulseCount} " +
                $"overlaps={string.Join(';', hurt.GetOverlappingAreas().Select(area => area.GetPath().ToString()))}");
        }
        try
        {
            health.Damaged += Damaged;
            shield.ShieldResolved += Guarded;
            Safety();
            float entryRight = entryCollision.ToGlobal(new Vector2(((RectangleShape2D)entryCollision.Shape).Size.X * .5f, 0)).X;
            float pistonLeft = pistonCollision.ToGlobal(new Vector2(-((RectangleShape2D)pistonCollision.Shape).Size.X * .5f, 0)).X;
            Check(entryRight == 2380 && pistonLeft == 2440 && pistonLeft - entryRight == 60 &&
                pistonCollision.ToGlobal(new Vector2(((RectangleShape2D)pistonCollision.Shape).Size.X * .5f, 0)).X == 2720 &&
                pistonCollision.ToGlobal(new Vector2(0, -40)).Y == 3916 && pistonCollision.ToGlobal(new Vector2(0, 40)).Y == 3996,
                "original-sized capsule has20px positive return clearance, with GearEntry and PistonOne right/top/bottom preserved");
            Check(Supported(ascent.GetNode<StaticBody2D>("Gameplay/Collision/SplitUpperGallery")) && Neutral(),
                "starts exactly where the seven-segment Temple return actually ended");
            PrologueCanyonTraversalTests.BeginTask10InputDrive(tree, player, Observe);
            for (int y = 1556; y <= 3476; y += 160)
            {
                await Reach(ascent, "ShortcutStep" + y);
                Check(motor.AcceptedJumpCount == jumps, "the original descending shaft uses no ordinary jumps");
            }
            for (int y = 3636; y <= 4116; y += 160)
            {
                await Reach(basin, "ShortcutStep" + y);
                Check(motor.AcceptedJumpCount == jumps, "all17 original descending steps remain walk/drop only");
            }
            await Reach(basin, "ShortcutBasinWalkway");
            // The independently authored lower SteamJet, not the right/left
            // diverter, crosses this return bypass. Respect its normal clock.
            // No valve/control, test clock advance, hazard disabling or HP write.
            phase = "lower-jet-safe-staging";
            await PrologueCanyonTraversalTests.Task4WalkTo(tree, player, 3570, 90);
            keyboard.ReleaseAll();
            for (int i = 0; i < 12; i++) await Tick();
            CollisionShape2D jetShape = lowerJet.Collision;
            Check(Supported(basin.GetNode<StaticBody2D>("Gameplay/Collision/ShortcutBasinWalkway")) && Neutral() &&
                player.GlobalPosition.X - 20 > 3496 && lowerJet.GlobalPosition == new Vector2(3460, 4110) &&
                jetShape.Shape is RectangleShape2D jetRectangle && jetRectangle.Size == new Vector2(72, 150) &&
                lowerJet.SafeSeconds == 1.25f && lowerJet.TelegraphSeconds == .45f && lowerJet.ActiveSeconds == .7f,
                "stages outside the independently measured original lower jet on its real unchanged bypass");
            float cycle = lowerJet.SafeSeconds + lowerJet.TelegraphSeconds + lowerJet.ActiveSeconds;
            int waits = 0;
            while (waits < 150 && (Mathf.PosMod(lowerJet.ElapsedSeconds, cycle) >= .035f || lowerJet.Frame.HazardActive))
            { await Tick(); waits++; }
            Check(waits < 150 && !lowerJet.Frame.HazardActive && !lowerJet.Monitoring && !lowerJet.Frame.Telegraphing,
                "waits for the actual next safe cycle within150, never rewinds or advances the hazard clock");
            GD.Print($"QUEST_FRAGMENT_RETURN_LOWER_JET_WAIT_OK physics={Engine.GetPhysicsFrames()} position={player.GlobalPosition} waits={waits} elapsed={lowerJet.ElapsedSeconds} nativeClock=true disabled=false");
            phase = "lower-jet-safe-crossing";
            await PrologueCanyonTraversalTests.Task4WalkTo(tree, player, 3360, 90);
            keyboard.ReleaseAll();
            for (int i = 0; i < 12; i++) await Tick();
            Check(player.GlobalPosition.X + 20 < 3424 && health.CurrentHealth == hp && !lowerJet.DisabledForRun,
                "crosses the complete jet interval on natural physical A before danger, retaining HP");
            phase = "walkway-west-return";
            await PrologueCanyonTraversalTests.Task4WalkTo(tree, player, 2530, 330);
            keyboard.ReleaseAll();
            for (int i = 0; i < 12; i++) await Tick();
            Check(Supported(basin.GetNode<StaticBody2D>("Gameplay/Collision/ShortcutBasinWalkway")) && Neutral(),
                "walks the real lower bypass without transport or unearned movement");
            await HopCampReturnStep();
            await HopGearEntry();
            phase = "camp-entry";
            await PrologueCanyonTraversalTests.Task4WalkTo(tree, player, 1980, 100);
            await SettleAndMark(basin.GetNode<StaticBody2D>("Gameplay/Collision/BasinCampFloor"));
            Check(segments == 21 && motor.AcceptedJumpCount == jumps + 3,
                "all17 shaft supports and four camp-return supports are witnessed with exactly three ordinary single jumps in one uninterrupted player lifetime");
            GD.Print($"QUEST_FRAGMENT_SPLIT_CAMP_KEYS player={playerId} jumps={motor.AcceptedJumpCount - jumps} dashes=0 K_edges=0 J_edges=0 L_edges={guardEdges} guards={guardedPulses} pulses={lane.PulseCount - lanePulsesBefore} otherGuards={otherGuards} rightAim={rightGuardValid.ToString().ToLowerInvariant()} fragments=1 steamOperated=false fullRoute=false");
        }
        catch (Exception error)
        {
            GD.Print($"QUEST_FRAGMENT_SPLIT_CAMP_FAILURE type={error.GetType().Name} {Facts()}");
            throw;
        }
        finally
        {
            if (GodotObject.IsInstanceValid(health)) health.Damaged -= Damaged;
            if (GodotObject.IsInstanceValid(shield)) shield.ShieldResolved -= Guarded;
            PrologueCanyonTraversalTests.EndTask10InputDrive(); keyboard.ReleaseAll();
        }
    }
}
