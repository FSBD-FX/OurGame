using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Knight.Abilities;
using Knight.Boss;
using Knight.Player;
using Knight.Prologue;
using Knight.Prologue.Collections;
using Knight.Prologue.Seamless;
using Knight.Prologue.Traversal;
using Knight.Shared;
using Knight.Combat;
using Knight.Mechanisms;
using Knight.Ui;
using Knight.World;
using Knight.Tests.Presentation;

namespace Knight.Tests.Integration;

public static partial class PrologueCanyonTraversalTests
{
    private static Action Task4FrameObserver;

    // Local saved-Main entrance geometry proof, not whole-route or human evidence.
    public static async Task RunEntranceEnclosure(SceneTree tree, bool presentation = false)
    {
        if (presentation) TestAssert.True(DisplayServer.GetName() != "headless" &&
            !string.IsNullOrWhiteSpace(RenderingServer.GetVideoAdapterName()) &&
            !new[] { "llvmpipe", "lavapipe", "swiftshader", "Microsoft Basic Render", "softpipe" }
                .Any(name => RenderingServer.GetVideoAdapterName().Contains(name, StringComparison.OrdinalIgnoreCase)),
            "entrance visual proof requires display-backed hardware rendering");
        var level = GD.Load<PackedScene>("res://scenes/prologue/PrologueLevel.tscn")
            .Instantiate<PrologueLevelController>();
        IsolatedPrologueFixture.Configure(level, "entrance-enclosure");
        var flow = level.GetNode<PrologueGameFlowController>("GameFlow");
        flow.HeadlessAutoStart = false;
        flow.AutomationAutoStart = false;
        var player = level.GetNode<CharacterBody2D>("PlayerRoot/Player");
        var input = player.GetNode<PlayerInputReader>("Input");
        var world = level.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors");
        ulong playerId = player.GetInstanceId(), worldId = world.GetInstanceId();
        int requests = 0, recoveries = 0;
        bool ceilingContact = false;
        bool ceilingObserverAttached = false;
        float lowestCeilingY = float.PositiveInfinity;
        // PhysicsFrame precedes the next motor update, so this captures the previous
        // tick's actual transient collision even if a rendered await skips it.
        void ObserveCeiling()
        {
            lowestCeilingY = Mathf.Min(lowestCeilingY, player.GlobalPosition.Y);
            for (int j = 0; j < player.GetSlideCollisionCount(); j++)
            {
                using KinematicCollision2D contact = player.GetSlideCollision(j);
                ceilingContact |= contact.GetNormal().Y > .7f &&
                    contact.GetCollider() is StaticBody2D body && body.Name == "LowBridgeCeiling";
            }
        }
        void Requested(string _) => requests++;
        void Recovered(string _) => recoveries++;
        level.RecoveryRequested += Requested;
        level.RecoveryCompleted += Recovered;
        tree.Root.AddChild(level);
        async Task Tick(int count = 1)
        {
            for (int i = 0; i < count; i++)
            {
                await KeyboardActionRuntimeTests.Frames(tree, input, 1);
                await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
                TestAssert.True(requests == 0 && recoveries == 0 &&
                    player.GetInstanceId() == playerId && world.GetInstanceId() == worldId &&
                    player.GetParent() == level.GetNode("PlayerRoot") &&
                    player.GetNode<HealthComponent>("Health").CurrentHealth > 0,
                    $"entrance proof retains the same living actor/world without recovery: " +
                    $"position={player.GlobalPosition} velocity={player.Velocity} " +
                    $"requests={requests} recoveries={recoveries} health={player.GetNode<HealthComponent>("Health").CurrentHealth}");
            }
        }
        async Task Capture(string stage)
        {
            if (!presentation) return;
            await tree.ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            string folder = System.IO.Path.Combine(IsolatedPrologueFixture.ValidateStorageRoot(), "entrance-proof");
            System.IO.Directory.CreateDirectory(folder);
            string target = System.IO.Path.Combine(folder, stage + ".png");
            TestAssert.True(!System.IO.File.Exists(target), "proof capture must not overwrite prior evidence");
            using Image frame = tree.Root.GetTexture().GetImage();
            TestAssert.Equal(Error.Ok, frame.SavePng(target), "save actual viewport proof");
            GD.Print($"ENTRANCE_VIEWPORT_SAVED stage={stage} renderer={RenderingServer.GetCurrentRenderingMethod()} " +
                $"gpu={RenderingServer.GetVideoAdapterName()} player={player.GlobalPosition} path={target}");
        }
        try
        {
            for (int i = 0; i < 3; i++) await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            TestAssert.True(flow.State == PrologueGameFlowState.Title &&
                level.GetNode<Button>("ProductUi/MainMenu/Panel/Buttons/Start").HasFocus(),
                "entrance proof begins at the normal focused title button");
            KeyboardActionRuntimeTests.KeyEvent(Key.Enter, true);
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            KeyboardActionRuntimeTests.KeyEvent(Key.Enter, false);
            for (int i = 0; i < 30 && flow.State != PrologueGameFlowState.Playing; i++)
                await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            TestAssert.Equal(PrologueGameFlowState.Playing, flow.State,
                "physical Enter starts the normal saved Main");
            await Tick(8);
            TestAssert.True(player.IsOnFloor() &&
                player.GlobalPosition.DistanceTo(new Vector2(160f, 600f)) < 2f,
                "normal entrance checkpoint has actual support, not a pre-tree pose override");
            await Capture("01-exterior-spawn");
            KeyboardActionRuntimeTests.KeyEvent(Key.A, true);
            bool wallContact = false;
            for (int i = 0; i < 45; i++)
            {
                await Tick();
                for (int j = 0; j < player.GetSlideCollisionCount(); j++)
                {
                    using KinematicCollision2D contact = player.GetSlideCollision(j);
                    wallContact |= contact.GetNormal().X > .7f &&
                        contact.GetCollider() is StaticBody2D body && body.Name == "EntranceOuterWall";
                }
                if (player.GlobalPosition.X < 20f) break;
            }
            KeyboardActionRuntimeTests.KeyEvent(Key.A, false);
            TestAssert.True(wallContact && player.GlobalPosition.X >= 68f - 1f &&
                player.IsOnFloor() && requests == 0 && recoveries == 0,
                "ENTRANCE_PERMANENT_WALL_RED: real A meets the visible permanent rock wall, not void or recovery");
            GD.Print($"ENTRANCE_OUTER_WALL_OK player={player.GlobalPosition} native_contact={wallContact} " +
                $"player_id={playerId} world_id={worldId} title=true recovery=0 saved_main=true");

            // The low remnant must be tested vertically on its own support.
            // A successful jump across its right edge need not touch its underside.
            KeyboardActionRuntimeTests.KeyEvent(Key.D, true);
            for (int i = 0; i < 60 && player.GlobalPosition.X < 260f; i++) await Tick();
            KeyboardActionRuntimeTests.KeyEvent(Key.D, false);
            await Tick(12);
            TestAssert.True(player.IsOnFloor() && player.GlobalPosition.X is > 250f and < 300f,
                "ceiling probe starts on the real near-bank support beneath the remnant");
            KeyboardActionRuntimeTests.KeyEvent(Key.Space, true);
            tree.PhysicsFrame += ObserveCeiling;
            ceilingObserverAttached = true;
            for (int i = 0; i < 80; i++)
            {
                await Tick();
                for (int j = 0; j < player.GetSlideCollisionCount(); j++)
                {
                    using KinematicCollision2D contact = player.GetSlideCollision(j);
                    ceilingContact |= contact.GetNormal().Y > .7f &&
                        contact.GetCollider() is StaticBody2D body && body.Name == "LowBridgeCeiling";
                }
                if (ceilingContact && player.IsOnFloor()) break;
            }
            KeyboardActionRuntimeTests.KeyEvent(Key.Space, false);
            await Tick(3);
            TestAssert.True(ceilingContact && player.IsOnFloor(),
                $"vertical jump meets the actual remnant underside then lands: contact={ceilingContact} position={player.GlobalPosition} " +
                $"minimum_y={lowestCeilingY} accepted_jumps={player.GetNode<PlayerMotor>("Motor").AcceptedJumpCount}");
            tree.PhysicsFrame -= ObserveCeiling;
            ceilingObserverAttached = false;
            GD.Print("ENTRANCE_CEILING_CONTACT_OK real_jump=true actual_normal=true natural_landing=true");

            KeyboardActionRuntimeTests.KeyEvent(Key.D, true);
            // StartFloor ends320; the actual20px capsule radius gives a300px
            // supported launch center. Starting280 under this low remnant is too early.
            for (int i = 0; i < 60 && player.GlobalPosition.X < 300f; i++) await Tick();
            TestAssert.True(player.IsOnFloor() && player.GlobalPosition.X is >= 295f and <= 310f,
                "entrance route reaches the actual near bank before its one erosion gap");
            int jumpsBefore = player.GetNode<PlayerMotor>("Motor").AcceptedJumpCount;
            KeyboardActionRuntimeTests.KeyEvent(Key.Space, true);
            bool farBankContact = false;
            for (int i = 0; i < 100; i++)
            {
                await Tick();
                if (player.GlobalPosition.X >= 440f) KeyboardActionRuntimeTests.KeyEvent(Key.D, false);
                for (int j = 0; j < player.GetSlideCollisionCount(); j++)
                {
                    using KinematicCollision2D contact = player.GetSlideCollision(j);
                    if (contact.GetCollider() is not StaticBody2D body) continue;
                    farBankContact |= body.Name == "BridgeFarBank" && contact.GetNormal().Y < -.7f;
                }
                if (farBankContact && player.GlobalPosition.X >= 440f) break;
            }
            KeyboardActionRuntimeTests.KeyEvent(Key.Space, false);
            KeyboardActionRuntimeTests.KeyEvent(Key.D, false);
            TestAssert.True(farBankContact && player.IsOnFloor() &&
                player.GetNode<PlayerMotor>("Motor").AcceptedJumpCount == jumpsBefore + 1,
                $"entrance gap uses one distinct real grounded jump and far-bank support: " +
                $"far_bank={farBankContact} floor={player.IsOnFloor()} " +
                $"jumps_before={jumpsBefore} jumps_after={player.GetNode<PlayerMotor>("Motor").AcceptedJumpCount} " +
                $"position={player.GlobalPosition} velocity={player.Velocity}");
            GD.Print($"ENTRANCE_EROSION_GAP_OK player={player.GlobalPosition} floor=true distinct_ground_jump=1");

            // J is real combat input while approaching the normally active first actor;
            // no actor, HP, AI, support or player-position write authorizes this route.
            KeyboardActionRuntimeTests.KeyEvent(Key.D, true);
            for (int i = 0; i < 150 && player.GlobalPosition.X < 900f; i++)
            {
                KeyboardActionRuntimeTests.KeyEvent(Key.J, i % 35 == 0);
                await Tick();
            }
            KeyboardActionRuntimeTests.KeyEvent(Key.J, false);
            KeyboardActionRuntimeTests.KeyEvent(Key.D, false);
            await Tick(3);
            TestAssert.True(player.GlobalPosition.X >= 895f && player.IsOnFloor() &&
                Mathf.Abs(player.GlobalPosition.Y - 600f) < 2f &&
                player.GetNode<PlayerMotor>("Motor").AcceptedJumpCount == jumpsBefore + 1,
                $"ENTRANCE_HORIZONTAL_MOUTH_RED: supported rock-mouth passage replaces the upward stair climb; actual={player.GlobalPosition}");
            GD.Print($"ENTRANCE_HORIZONTAL_MOUTH_OK player={player.GlobalPosition} " +
                $"health={player.GetNode<HealthComponent>("Health").CurrentHealth} position_writes=0 extra_jump=0");
            await Capture("02-rock-mouth");

            TestAssert.True(world.TryGetMountedSector(PrologueSectorId.CanyonEntrance, out var entrance),
                "normal entrance remains genuinely mounted");
            var roof = entrance.GetNode<StaticBody2D>("Gameplay/Collision/RiftEntryCeiling");
            var roofProfile = roof.GetNode<GrappleSurface2D>("GrappleSurfaceProfile");
            var claw = player.GetNode<MechanicalClawController>("MechanicalClaw");
            await Tick(35); // Finish the actual last J, without writing its state.
            float beforePullY = player.GlobalPosition.Y;
            KeyboardActionRuntimeTests.KeyEvent(Key.W, true);
            await Tick(3);
            KeyboardActionRuntimeTests.KeyEvent(Key.K, true);
            await Tick(2);
            KeyboardActionRuntimeTests.KeyEvent(Key.K, false);
            for (int i = 0; i < 90 && !(claw.CurrentTarget == roofProfile &&
                claw.CurrentFrame.State is (MechanicalClawState.Reel or MechanicalClawState.Swing) &&
                player.GlobalPosition.Y < beforePullY - 80f); i++) await Tick();
            TestAssert.True(claw.CurrentTarget == roofProfile &&
                claw.CurrentFrame.LockedTargetRuntimeId == roofProfile.GetInstanceId() &&
                claw.CurrentFrame.CastId > 0UL && Mathf.Abs(claw.CurrentFrame.AnchorPoint.Y - 400f) < 1f &&
                player.GlobalPosition.Y < beforePullY - 80f,
                "real W/K locks the authored roof collision face and physically auto-pulls the same actor");
            GD.Print($"ENTRANCE_ROOF_CLAW_OK cast={claw.CurrentFrame.CastId} " +
                $"target={claw.CurrentFrame.LockedTargetRuntimeId} anchor={claw.CurrentFrame.AnchorPoint} " +
                $"from_y={beforePullY} player={player.GlobalPosition}");
            await Capture("03-actual-roof-pull");
            KeyboardActionRuntimeTests.KeyEvent(Key.K, true);
            await Tick(2);
            KeyboardActionRuntimeTests.KeyEvent(Key.K, false);
            KeyboardActionRuntimeTests.KeyEvent(Key.W, false);
            for (int i = 0; i < 100 && !(claw.CurrentFrame.State == MechanicalClawState.Idle && player.IsOnFloor()); i++)
                await Tick();
            TestAssert.True(claw.CurrentFrame.State == MechanicalClawState.Idle && player.IsOnFloor(),
                "fresh K cancel naturally returns to the entrance floor");

            // Diagnostic material-only negative on this fixture's real collider.
            // Alternative legal targets are permitted, but the ice roof cannot lock.
            GrappleSurfaceMaterial oldMaterial = roofProfile.Material;
            ulong negativeNextCast = claw.CurrentFrame.NextCastId;
            bool negativeAim = false, negativeAttempt = false, negativeRoofLock = false;
            int negativeSamples = 0;
            void ObserveNegative(MechanicalClawFrame frame, GrappleSurface2D target)
            {
                negativeSamples++;
                negativeAim |= frame.State == MechanicalClawState.Aim && frame.NextCastId == negativeNextCast;
                // No valid target legitimately fails BEFORE assigning a CastId.
                // Legal alternate targets must use this new cast, not the old roof cast.
                negativeAttempt |= (negativeAim && frame.State == MechanicalClawState.Failed &&
                    frame.Failure != MechanicalClawFailure.None) ||
                    (frame.CastId == negativeNextCast && frame.State is
                        (MechanicalClawState.Windup or MechanicalClawState.Travel or MechanicalClawState.Latched));
                negativeRoofLock |= frame.LockedTargetRuntimeId == roofProfile.GetInstanceId() ||
                    (target == roofProfile && frame.State is (MechanicalClawState.Travel or
                        MechanicalClawState.Latched or MechanicalClawState.Reel or MechanicalClawState.Swing or MechanicalClawState.Boost));
            }
            claw.ClawFrameChanged += ObserveNegative;
            try
            {
                roofProfile.Material = GrappleSurfaceMaterial.Ice;
                KeyboardActionRuntimeTests.KeyEvent(Key.W, true);
                await Tick(3);
                KeyboardActionRuntimeTests.KeyEvent(Key.K, true);
                await Tick(2);
                KeyboardActionRuntimeTests.KeyEvent(Key.K, false);
                await Tick(45);
                TestAssert.True(negativeAim && negativeAttempt && negativeSamples >= 2,
                    "ENTRANCE_ICE_ATTEMPT_REQUIRED: a fresh K is accepted into Aim then attempts resolution, not an ignored key");
                TestAssert.True(!negativeRoofLock && claw.CurrentTarget != roofProfile &&
                    claw.CurrentFrame.LockedTargetRuntimeId != roofProfile.GetInstanceId(),
                    "same Ice collider never locks anywhere in this entire new K publication window");
                GD.Print($"ENTRANCE_ROOF_ICE_CONTROL_OK diagnostic_material_only=true alternatives_allowed=true " +
                    $"new_aim={negativeAim} new_attempt={negativeAttempt} samples={negativeSamples} roof_lock={negativeRoofLock}");
            }
            finally
            {
                roofProfile.Material = oldMaterial;
                claw.ClawFrameChanged -= ObserveNegative;
                KeyboardActionRuntimeTests.KeyEvent(Key.W, false);
                KeyboardActionRuntimeTests.KeyEvent(Key.K, false);
            }
            if (claw.CurrentFrame.State != MechanicalClawState.Idle)
            {
                KeyboardActionRuntimeTests.KeyEvent(Key.K, true);
                await Tick(2);
                KeyboardActionRuntimeTests.KeyEvent(Key.K, false);
            }
            for (int i = 0; i < 100 && !(claw.CurrentFrame.State == MechanicalClawState.Idle && player.IsOnFloor()); i++)
                await Tick();
            TestAssert.True(claw.CurrentFrame.State == MechanicalClawState.Idle && player.IsOnFloor(),
                "negative-control cleanup returns through real input to supported Idle");

            KeyboardActionRuntimeTests.KeyEvent(Key.D, true);
            for (int i = 0; i < 150 && player.GlobalPosition.X < 1360f; i++) await Tick();
            KeyboardActionRuntimeTests.KeyEvent(Key.D, false);
            await Tick(12);
            TestAssert.True(world.TryGetMountedSector(PrologueSectorId.CanyonDescent, out var descent),
                "new entry connects to the actual resident Descent");
            bool upperSupport = false;
            for (int j = 0; j < player.GetSlideCollisionCount(); j++)
            {
                using KinematicCollision2D contact = player.GetSlideCollision(j);
                upperSupport |= contact.GetCollider() == descent.GetNode<StaticBody2D>("Gameplay/Collision/RiftUpperFloor") &&
                    contact.GetNormal().Y < -.7f;
            }
            TestAssert.True(level.CurrentSector == PrologueSectorId.CanyonDescent && upperSupport &&
                Mathf.Abs(player.GlobalPosition.Y - 600f) < 2f,
                "normal input crosses the600px seam onto real rift support without a stage switch or warp");
            GD.Print($"ENTRANCE_RIFT_SEAM_OK player={player.GlobalPosition} support=true persistent_actor=true");
            await Capture("04-continuous-rift");
            KeyboardActionRuntimeTests.KeyEvent(Key.A, true);
            for (int i = 0; i < 150 && player.GlobalPosition.X > 900f; i++) await Tick();
            KeyboardActionRuntimeTests.KeyEvent(Key.A, false);
            await Tick(12);
            TestAssert.True(level.CurrentSector == PrologueSectorId.CanyonEntrance && player.IsOnFloor() &&
                player.GlobalPosition.X < 910f && flow.State == PrologueGameFlowState.Playing &&
                flow.Save.CheckpointId == "canyon.v2.start" &&
                flow.Save.CheckpointWorldPosition == new Vector2(160f, 600f) &&
                !flow.Save.HasActivatedCheckpoint && requests == 0 && recoveries == 0,
                "real reverse traversal preserves the stable start checkpoint and continuous actor/world");
            GD.Print($"ENTRANCE_RIFT_ROUNDTRIP_OK player={player.GlobalPosition} " +
                $"health={player.GetNode<HealthComponent>("Health").CurrentHealth} recovery=0 checkpoint=start " +
                "position_writes=0 save_migration_proved=false whole_goal=false");

            bool SupportedBy(string name)
            {
                var body = descent.GetNode<StaticBody2D>("Gameplay/Collision/" + name);
                for (int j = 0; j < player.GetSlideCollisionCount(); j++)
                {
                    using KinematicCollision2D contact = player.GetSlideCollision(j);
                    if (contact.GetCollider() == body && contact.GetNormal().Y < -.7f) return true;
                }
                return false;
            }
            async Task WalkTo(float x, int limit)
            {
                Key direction = player.GlobalPosition.X < x ? Key.D : Key.A;
                KeyboardActionRuntimeTests.KeyEvent(direction, true);
                for (int i = 0; i < limit &&
                    (direction == Key.D ? player.GlobalPosition.X < x : player.GlobalPosition.X > x); i++)
                    await Tick();
                KeyboardActionRuntimeTests.KeyEvent(direction, false);
                await Tick(8);
            }
            async Task LandOn(string body, string message)
            {
                for (int i = 0; i < 100 && !SupportedBy(body); i++) await Tick();
                TestAssert.True(SupportedBy(body), $"{message}: actual={player.GlobalPosition} support={body}");
                GD.Print($"ENTRANCE_NATURAL_DROP_OK body={body} player={player.GlobalPosition} recovery=0");
            }
            await WalkTo(1370f, 150);
            await LandOn("RiftUpperFloor", "second entry retains the actual upper floor");
            await WalkTo(1940f, 150);
            await LandOn("RiftMiddleCatch", "normal right departure lands on the middle shelf");
            await WalkTo(1460f, 150);
            await LandOn("RiftLowerCatch", "normal left departure lands on the lower shelf");
            await Capture("05-lower-rift");
            await WalkTo(1350f, 80);
            await LandOn("ArchiveEntry", "ENTRANCE_ARCHIVE_ACCESS_RED: authored branch admits the unchanged40px capsule");
            GD.Print($"ENTRANCE_ARCHIVE_ACCESS_OK player={player.GlobalPosition} position_writes=0 recovery=0");
            await WalkTo(1610f, 100);
            await LandOn("ArchiveShelf", "archive entry descends onto its intended supply approach");
            await Capture("05b-expanded-archive-shelf");
            // The wider shelf starts1480;1520 is now deliberately supported,
            // not a walk-off point. Leave its real left edge, without a warp.
            await WalkTo(1440f, 80);
            await LandOn("ArchiveFloor", "archive approach retains its actual floor");
            await WalkTo(1350f, 100);
            await LandOn("FrozenLeftCatch", "ENTRANCE_ARCHIVE_EXIT_RED: real branch opening reaches the existing frozen catch");
            TestAssert.True(player.GlobalPosition.X is >= 1300f and <= 1380f,
                "frozen branch departure keeps the whole unchanged capsule inside the sector");
            GD.Print($"ENTRANCE_ARCHIVE_FROZEN_EXIT_OK player={player.GlobalPosition} " +
                $"health={player.GetNode<HealthComponent>("Health").CurrentHealth} position_writes=0 recovery=0");
            await Capture("06-archive-exit");
        }
        finally
        {
            foreach (Key key in new[] { Key.Enter, Key.A, Key.D, Key.W, Key.Space, Key.J, Key.K, Key.L })
                KeyboardActionRuntimeTests.KeyEvent(key, false);
            level.RecoveryRequested -= Requested;
            level.RecoveryCompleted -= Recovered;
            if (ceilingObserverAttached) tree.PhysicsFrame -= ObserveCeiling;
            tree.Paused = false;
            await TestSceneTeardown.FreeSceneAfterAudioDrain(tree, level);
        }
    }

    internal static void BeginTask10InputDrive(SceneTree tree, CharacterBody2D player,
        Action observe)
    {
        ActiveTree = tree;
        ActiveInputCanvas = player;
        ReleaseAll();
        Task4FrameObserver = observe;
    }

    internal static void EndTask10InputDrive()
    {
        Task4FrameObserver = null;
        ReleaseAll();
    }

    internal static async Task DriveTask4GeneratedMain(SceneTree tree, SeamlessPrologueWorld world,
        CharacterBody2D player, Action observe, Func<string,Task> milestone)
    {
        ActiveTree=tree; ActiveInputCanvas=player; ReleaseAll(); Task4FrameObserver=observe;
        PrologueSector Sector(PrologueSectorId id) => world.TryGetMountedSector(id,out var sector)
            ? sector : throw new InvalidOperationException($"actual main route sector {id} is not mounted");
        StaticBody2D Body(PrologueSectorId id,string name) => Sector(id).GetNode<StaticBody2D>("Gameplay/Collision/"+name);
        try
        {
            await WaitForFloor(tree,player);
            await milestone("01-supported-entrance");
            await Task4WalkTo(tree,player,300,80);
            await Task4ReachSupport(tree,player,Body(PrologueSectorId.CanyonEntrance,"BridgeFarBank"),false);
            for(int i=1;i<=7;i++) await Task4ReachSupport(tree,player,Body(PrologueSectorId.CanyonEntrance,$"BridgeExitStep{i}"),false);
            await Task4WalkTo(tree,player,1360,100);
            await Task4WaitSupport(tree,player,Sector(PrologueSectorId.CanyonDescent),"RiftUpperFloor");
            await Task4WalkTo(tree,player,1940,180);
            await Task4WaitSupport(tree,player,Sector(PrologueSectorId.CanyonDescent),"RiftMiddleCatch");
            await Task4WalkTo(tree,player,1460,180);
            await Task4WaitSupport(tree,player,Sector(PrologueSectorId.CanyonDescent),"RiftLowerCatch");
            await Task4WalkTo(tree,player,2140,210);
            await Task4WaitSupport(tree,player,Sector(PrologueSectorId.CanyonDescent),"AbyssCommitment");
            await milestone("02-abyss-commitment");
            SendAction(InputActions.MoveRight,true);
            for(int i=0;i<60 && player.IsOnFloor();i++) await PhysicsFrames(tree,1);
            SendAction(InputActions.MoveRight,false);
            var bottom=Body(PrologueSectorId.CanyonDescent,"AbyssSafeBottom");
            for(int i=0;i<500 && !Task4HasFloorContact(player,bottom);i++) await PhysicsFrames(tree,1);
            TestAssert.True(Task4HasFloorContact(player,bottom),"generated-main passive fall reaches actual safe bottom");
            await milestone("03-safe-bottom");
            await Task4WalkTo(tree,player,1860,140);
            await Task4WaitSupport(tree,player,Sector(PrologueSectorId.CanyonBasin),"BasinCampFloor");
            await milestone("04-basin-camp");
            await Task4WalkTo(tree,player,1940,60);
            foreach(string name in new[]{"GearEntry","GearPistonOne","GearPistonTwo","UpperGearGalleryOne","UpperGearGalleryTwo","UpperGearGalleryThree"})
                await Task4ReachSupport(tree,player,Body(PrologueSectorId.CanyonBasin,name),false);
            await Task4ReachSupport(tree,player,Body(PrologueSectorId.CanyonAscent,"LiftEntryFloor"),false);
            await Task4ReachSupport(tree,player,Body(PrologueSectorId.CanyonBasin,"ShortcutStep3636"),true);
            await Task4WalkTo(tree,player,4276,70);
            foreach(string name in new[]{"LiftAccessStepOne","LiftAccessStepTwo","LiftBackLanding"})
                await Task4ReachSupport(tree,player,Body(PrologueSectorId.CanyonAscent,name),false);
            await milestone("05-public-lift-junction");
            for(int i=1;i<=9;i++) await Task4ReachSupport(tree,player,Body(PrologueSectorId.CanyonAscent,$"AscentGrip{i}"),true);
            await Task4ReachSupport(tree,player,Body(PrologueSectorId.CanyonAscent,"AscentFinalCatch"),true);
            await Task4ReachSupport(tree,player,Body(PrologueSectorId.CanyonAscent,"CourtyardApproach"),false);
            await Task4WalkTo(tree,player,5220,100);
            await Task4WaitSupport(tree,player,Sector(PrologueSectorId.LostTemple),"CourtyardFloor");
            await milestone("06-courtyard");
            await Task4ReachSupport(tree,player,Body(PrologueSectorId.LostTemple,"CourtyardStep"),false);
            await Task4ReachSupport(tree,player,Body(PrologueSectorId.LostTemple,"CheckpointFloor"),false);
            var chain=Sector(PrologueSectorId.LostTemple).GetNode<BellChainMechanism>("Gameplay/Mechanisms/BellChain");
            var chainTarget=chain.GetNode<GrappleSurface2D>("ThreadTarget");
            Vector2 chainAim=chainTarget.WorldAnchor;
            TestAssert.True(player.GetViewportRect().HasPoint(player.GetCanvasTransform()*chainAim),"real BellChain interaction point is visible");
            SendAction(InputActions.SelectGrappleMode,true); await PhysicsFrames(tree,1); SendAction(InputActions.SelectGrappleMode,false);
            SendMouse(MouseButton.Left,true,chainAim); await PhysicsFrames(tree,1); SendMouse(MouseButton.Left,false,chainAim);
            for(int i=0;i<150 && !chain.IsOpen;i++) await PhysicsFrames(tree,1);
            TestAssert.True(chain.IsOpen,"generated main opens the real BellChain gate through E and normal mouse input");
            GD.Print($"TASK4_GENERATED_MAIN_BELL_INPUT_OK target={chainAim} player={player.GlobalPosition}");
            ReleaseAll(); await PhysicsFrames(tree,4);
            await Task4WalkTo(tree,player,5920,140);
            await Task4WaitSupport(tree,player,Sector(PrologueSectorId.LostTemple),"CheckpointFloor");
            TestAssert.Equal(0,player.GetNode<PlayerMotor>("Motor").AcceptedDashCount,"generated v2 main does not borrow air dash");
            await milestone("07-pre-boss-checkpoint");
        }
        finally { Task4FrameObserver=null; ReleaseAll(); }
    }

    public static async Task RunTask4(SceneTree tree)
    {
        ActiveTree = tree;
        await AbyssBodyProbe(tree, -1);
        for (int exit = 0; exit < 3; exit++) await AbyssBodyProbe(tree, exit);
        GD.Print("TASK4_ABYSS_PHYSICS_OK passive=True right_brake=True left_brake=True exits=3 input=True capsule_radius=20 capsule_height=72");
    }

    public static async Task RunTask4FirstLessons(SceneTree tree)
    {
        ActiveTree = tree;
        foreach (bool bridge in new[] { true, false })
        {
            ReleaseAll();
            var root = new Node2D { Name = "Task4FirstLesson" };
            var sector = GD.Load<PackedScene>($"res://scenes/prologue/sectors/{(bridge ? "CanyonEntrance" : "CanyonDescent")}.tscn").Instantiate<PrologueSector>();
            sector.Position = CanyonWorldLayout.SectorBounds[sector.SectorId].Position;
            var player = NewPlayer();
            player.Position = bridge ? new(280,600) : new(1360,64);
            player.GetNode<Camera2D>("Camera2D").Enabled = true;
            root.AddChild(sector); root.AddChild(player); tree.Root.AddChild(root);
            ActiveInputCanvas = player;
            try
            {
                var capsule = (CapsuleShape2D)player.GetNode<CollisionShape2D>("BodyCollider").Shape;
                TestAssert.Equal(20f, capsule.Radius, "all lesson probes use the actual 20px radius capsule");
                TestAssert.Equal(72f, capsule.Height, "all lesson probes use the actual 72px total-height capsule");
                await WaitForFloor(tree, player);
                if (bridge)
                {
                    await Task4WalkTo(tree, player, 300, 30);
                    SendAction(InputActions.MoveRight,true); SendAction(InputActions.Jump,true);
                    bool landed = false;
                    for (int i=0; i<120; i++)
                    {
                        await PhysicsFrames(tree,1);
                        if (player.GlobalPosition.X >= 420) SendAction(InputActions.MoveRight,false);
                        if (player.GlobalPosition.X >=420 && Task4HasFloorContact(player,sector.GetNode<StaticBody2D>("Gameplay/Collision/BridgeFarBank")))
                        { landed=true; break; }
                        if (player.GlobalPosition.Y>720) break;
                    }
                    TestAssert.True(landed,"LowCeilingBridgeCrossesWithOneGroundJump: actual capsule must reach far-bank support");
                    TestAssert.True(player.GlobalPosition.X<=480,"bridge landing includes the full 20px horizontal capsule inset");
                    TestAssert.Equal(1,player.GetNode<PlayerMotor>("Motor").AcceptedJumpCount,"bridge uses exactly one grounded jump, never double jump");
                    GD.Print($"TASK4_BRIDGE_INPUT_OK position={player.GlobalPosition} actual_floor=True jumps=1");
                }
                else
                {
                    await Task4WalkTo(tree,player,1940,180);
                    await Task4WaitSupport(tree,player,sector,"RiftMiddleCatch");
                    // Continuing right meets a genuine wall, rather than a metadata-only hint.
                    SendAction(InputActions.MoveRight,true); await PhysicsFrames(tree,90);
                    TestAssert.True(player.GlobalPosition.X <= 2180.1f && player.IsOnFloor(),"rift middle cannot bypass alternating transfer by continuing right");
                    await Task4WalkTo(tree,player,1460,210);
                    await Task4WaitSupport(tree,player,sector,"RiftLowerCatch");
                    await Task4WalkTo(tree,player,2140,210);
                    await Task4WaitSupport(tree,player,sector,"AbyssCommitment");
                    TestAssert.Equal(0,player.GetNode<PlayerMotor>("Motor").AcceptedJumpCount,"rift uses real staggered catches, not extra jumps");
                    GD.Print($"TASK4_RIFT_INPUT_OK reversals=2 catches=3 commitment={player.GlobalPosition}");
                }
                TestAssert.Equal(0,player.GetNode<PlayerMotor>("Motor").AcceptedDashCount,"first lessons never borrow air dash");
            }
            finally { ReleaseAll(); root.QueueFree(); await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame); await PhysicsFrames(tree,3); }
        }
    }

    internal static async Task Task4CrossEntranceGap(SceneTree tree, CharacterBody2D player, StaticBody2D farBank)
    {
        // The enlarged bank's CENTER is840, beyond the first encounter's740
        // residency limit. Land at440, not at the center of a long rectangle.
        TestAssert.True(player.IsOnFloor() && player.GlobalPosition.X is >= 295f and <= 310f,
            "erosion crossing starts on the actual supported near-bank edge");
        var motor = player.GetNode<PlayerMotor>("Motor");
        int jumps = motor.AcceptedJumpCount, dashes = motor.AcceptedDashCount;
        try
        {
            SendAction(InputActions.MoveRight, true);
            SendAction(InputActions.Jump, true);
            for (int i = 0; i < 100; i++)
            {
                await PhysicsFrames(tree, 1);
                if (player.GlobalPosition.X >= 440f) SendAction(InputActions.MoveRight, false);
                if (player.GlobalPosition.X >= 440f && Task4HasFloorContact(player, farBank)) break;
            }
            TestAssert.True(player.GlobalPosition.X is >= 440f and < 500f &&
                Task4HasFloorContact(player, farBank) && motor.AcceptedJumpCount == jumps + 1 &&
                motor.AcceptedDashCount == dashes,
                $"actual erosion gap uses one ground jump onto the near end of the real bank: {player.GlobalPosition}");
            GD.Print($"ENTRANCE_ROUTE_BANK_OK player={player.GlobalPosition} one_ground_jump=true center_shortcut=false");
        }
        finally
        {
            SendAction(InputActions.Jump, false);
            SendAction(InputActions.MoveRight, false);
        }
    }

    internal static async Task Task4WalkTo(SceneTree tree, CharacterBody2D player, float x, int budget)
    {
        int direction = x > player.GlobalPosition.X ? 1 : -1;
        var samples = new List<string>();
        void ObserveWalk(int frame)
        {
            if(KeyboardRouteInputDriver.Current==null || samples.Count>=12) return;
            var input=player.GetNode<PlayerInputReader>("Input");
            var health=player.GetNode<HealthComponent>("Health");
            var shield=player.GetNode<PlayerShieldController>("Shield");
            samples.Add($"frame={frame} player={player.GlobalPosition} velocity={player.Velocity} " +
                $"floor={player.IsOnFloor()} hp={health.CurrentHealth} move={input.Current.MoveAxis} " +
                $"guard={shield.IsActive}/{shield.GuardDirection} heldL={input.Current.ShieldHeld}");
        }
        SendAction(InputActions.MoveLeft,direction<0); SendAction(InputActions.MoveRight,direction>0);
        for (int i=0; i<budget && (x-player.GlobalPosition.X)*direction > 2; i++)
        {
            if(i%10==0) ObserveWalk(i);
            await PhysicsFrames(tree,1);
        }
        SendAction(InputActions.MoveLeft,false); SendAction(InputActions.MoveRight,false);
        ObserveWalk(budget);
        TestAssert.True((x-player.GlobalPosition.X)*direction<=2,
            $"real walking reaches x={x}; actual={player.GlobalPosition}; samples=[{string.Join(";",samples)}]");
    }

    internal static async Task Task4WaitSupport(SceneTree tree, CharacterBody2D player, PrologueSector sector, string name)
    {
        var support = sector.GetNode<StaticBody2D>("Gameplay/Collision/"+name);
        for(int i=0;i<90;i++)
        {
            await PhysicsFrames(tree,1);
            if(Task4HasFloorContact(player,support)) return;
        }
        throw new InvalidOperationException($"real capsule failed to contact {name}; position={player.GlobalPosition}");
    }

    public static async Task RunTask4ShortcutTolerance(SceneTree tree)
    {
        foreach(float offset in new[]{-20f,20f})
            await RunTask4Shortcut(tree,true,offset);
        GD.Print("TASK4_MIRRORED_TRANSFER_TOLERANCE_OK offsets=-20,+20 actual_walk=True");
    }

    public static async Task RunTask4Shortcut(SceneTree tree, bool firstTransferOnly = false, float? mirroredLaunchOffset = null)
    {
        ActiveTree = tree; ReleaseAll();
        var root = new Node2D { Name = "Task4PhysicalShortcut" };
        var sectors = new Dictionary<PrologueSectorId, PrologueSector>();
        foreach (PrologueSectorId id in new[] { PrologueSectorId.CanyonBasin, PrologueSectorId.CanyonAscent })
        {
            var sector = GD.Load<PackedScene>($"res://scenes/prologue/sectors/{id}.tscn").Instantiate<PrologueSector>();
            sector.Position = CanyonWorldLayout.SectorBounds[id].Position;
            sectors.Add(id,sector); root.AddChild(sector);
        }
        var player = NewPlayer(); player.Position = new(1680,3960);
        player.GetNode<Camera2D>("Camera2D").Enabled = true;
        root.AddChild(player); tree.Root.AddChild(root); ActiveInputCanvas = player;
        try
        {
            await WaitForFloor(tree,player);
            await Task4WalkTo(tree,player,2500,260);
            await Task4WaitSupport(tree,player,sectors[PrologueSectorId.CanyonBasin],"ShortcutBasinWalkway");
            await Task4WalkTo(tree,player,3800,350);
            await Task4ReachSupport(tree,player,sectors[PrologueSectorId.CanyonBasin].GetNode<StaticBody2D>("Gameplay/Collision/ShortcutStep4116"),false);
            for(int y=3956;y>=3636;y-=160)
            {
                await Task4ReachSupport(tree,player,sectors[PrologueSectorId.CanyonBasin].GetNode<StaticBody2D>($"Gameplay/Collision/ShortcutStep{y}"),true);
                if(y==3956 && mirroredLaunchOffset.HasValue)
                {
                    float launchBefore=player.GlobalPosition.X;
                    await Task4WalkTo(tree,player,launchBefore+mirroredLaunchOffset.Value,40);
                    await PhysicsFrames(tree,8);
                    var support=sectors[PrologueSectorId.CanyonBasin].GetNode<StaticBody2D>("Gameplay/Collision/ShortcutStep3956");
                    TestAssert.True(Task4HasFloorContact(player,support) && player.GlobalPosition.X>=4120 && player.GlobalPosition.X<=4230,
                        "tolerance starts from complete real support after normal A/D, not a placement write");
                    TestAssert.True(MathF.Abs(player.GlobalPosition.X-launchBefore)>=18,
                        "tolerance launch differs by at least18px through real movement");
                    GD.Print($"TASK4_MIRRORED_TOLERANCE_START offset={mirroredLaunchOffset.Value} before={launchBefore} actual={player.GlobalPosition}");
                }
                if (firstTransferOnly && (!mirroredLaunchOffset.HasValue || y==3796))
                {
                    GD.Print("TASK4_SINGLE_TRANSFER_OK full_capsule_inset=True actual_support=True normal_input=True");
                    return;
                }
            }
            for(int y=3476;y>=1556;y-=160)
                await Task4ReachSupport(tree,player,sectors[PrologueSectorId.CanyonAscent].GetNode<StaticBody2D>($"Gameplay/Collision/ShortcutStep{y}"),true);
            await Task4ReachSupport(tree,player,sectors[PrologueSectorId.CanyonAscent].GetNode<StaticBody2D>("Gameplay/Collision/SplitUpperGallery"),false);
            TestAssert.True(player.GlobalPosition.Y < 1481 && player.GlobalPosition.X >=3840,
                "BasinShortcutReturnsToUpperGallery: real capsule/input reaches upper gallery from basin camp");
            GD.Print($"TASK4_SHORTCUT_UP_OK position={player.GlobalPosition} no_teleport=True");
            // Descend the same actual alternating supports, with each transfer independent.
            for(int y=1556;y<=3476;y+=160)
                await Task4ReachSupport(tree,player,sectors[PrologueSectorId.CanyonAscent].GetNode<StaticBody2D>($"Gameplay/Collision/ShortcutStep{y}"),false);
            for(int y=3636;y<=4116;y+=160)
                await Task4ReachSupport(tree,player,sectors[PrologueSectorId.CanyonBasin].GetNode<StaticBody2D>($"Gameplay/Collision/ShortcutStep{y}"),false);
            await Task4WalkTo(tree,player,3780,100);
            await Task4WaitSupport(tree,player,sectors[PrologueSectorId.CanyonBasin],"ShortcutBasinWalkway");
            await Task4WalkTo(tree,player,2530,330);
            await Task4ReachSupport(tree,player,sectors[PrologueSectorId.CanyonBasin].GetNode<StaticBody2D>("Gameplay/Collision/ShortcutCampReturnStep"),false);
            await Task4ReachSupport(tree,player,sectors[PrologueSectorId.CanyonBasin].GetNode<StaticBody2D>("Gameplay/Collision/GearEntry"),false);
            await Task4WalkTo(tree,player,1980,100);
            await Task4WaitSupport(tree,player,sectors[PrologueSectorId.CanyonBasin],"BasinCampFloor");
            TestAssert.True(player.GlobalPosition.X<2080 && player.GlobalPosition.Y<4000,
                "UpperGalleryShortcutReachesBasin: real capsule returns to camp support without teleport");
            TestAssert.Equal(0,player.GetNode<PlayerMotor>("Motor").AcceptedDashCount,"shortcut uses no unearned air dash");
            GD.Print($"TASK4_SHORTCUT_DOWN_OK position={player.GlobalPosition} no_teleport=True");
        }
        finally { ReleaseAll(); root.QueueFree(); await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame); await PhysicsFrames(tree,3); }
    }

    public static async Task RunTask4GearConnectors(SceneTree tree)
    {
        ActiveTree=tree;
        var failures = new List<Exception>();
        foreach(bool lower in new[]{false,true})
        {
            ReleaseAll();
            var root=new Node2D {Name="Task4GearConnector"};
            var basin=GD.Load<PackedScene>("res://scenes/prologue/sectors/CanyonBasin.tscn").Instantiate<PrologueSector>();
            var ascent=GD.Load<PackedScene>("res://scenes/prologue/sectors/CanyonAscent.tscn").Instantiate<PrologueSector>();
            basin.Position=new(1280,3600); ascent.Position=new(3840,720);
            var player=NewPlayer(); player.Position=new(2920,3800); player.GetNode<Camera2D>("Camera2D").Enabled=true;
            root.AddChild(basin);root.AddChild(ascent);root.AddChild(player);tree.Root.AddChild(root);ActiveInputCanvas=player;
            try
            {
                await WaitForFloor(tree,player);
                if(!lower)
                {
                    Rect2 steam=CanyonWorldLayout.Nodes.Single(n=>n.Id==CanyonRouteNodeId.SteamValves).WorldBounds;
                    Task4FrameObserver=()=>TestAssert.True(!steam.Intersects(new Rect2(player.GlobalPosition-new Vector2(20,36),new Vector2(40,72))),
                        "upper Gear-to-Lift route keeps the whole capsule outside Steam bounds");
                    foreach(string name in new[]{"UpperGearGalleryOne","UpperGearGalleryTwo","UpperGearGalleryThree"})
                        await Task4ReachSupport(tree,player,basin.GetNode<StaticBody2D>("Gameplay/Collision/"+name),false);
                    await Task4ReachSupport(tree,player,ascent.GetNode<StaticBody2D>("Gameplay/Collision/LiftEntryFloor"),false);
                    GD.Print($"TASK4_UPPER_GEAR_LIFT_INPUT_OK position={player.GlobalPosition}");
                }
                else
                {
                    await Task4WalkTo(tree,player,2760,80);
                    await Task4WaitSupport(tree,player,basin,"ShortcutBasinWalkway");
                    await Task4WalkTo(tree,player,3800,270);
                    await Task4ReachSupport(tree,player,basin.GetNode<StaticBody2D>("Gameplay/Collision/ShortcutStep4116"),false);
                    await Task4WalkTo(tree,player,4040,100);
                    await Task4WaitSupport(tree,player,basin,"SteamTrenchFloor");
                    await Task4WalkTo(tree,player,3700,130);
                    TestAssert.True(CanyonWorldLayout.Nodes.Single(n=>n.Id==CanyonRouteNodeId.SteamValves).WorldBounds.HasPoint(player.GlobalPosition),
                        "GearPistonsToSteamValves: real capsule physically reaches lower Steam node, not just a graph edge");
                    GD.Print($"TASK4_GEAR_STEAM_INPUT_OK position={player.GlobalPosition}");
                    await Task4WalkTo(tree,player,4090,130);
                    for(int i=1;i<=8;i++) await Task4ReachSupport(tree,player,basin.GetNode<StaticBody2D>($"Gameplay/Collision/SteamRejoinStep{i}"),false);
                    await Task4WalkTo(tree,player,5050,70);
                    TestAssert.True(player.GlobalPosition.Y<3600,"SteamValvesToCounterweightLift: lower physical branch rejoins actual lift height");
                    GD.Print($"TASK4_STEAM_LIFT_INPUT_OK position={player.GlobalPosition}");
                    await Task4ReachSupport(tree,player,ascent.GetNode<StaticBody2D>("Gameplay/Collision/LiftBackLanding"),true);
                    await Task4ReachSupport(tree,player,ascent.GetNode<StaticBody2D>("Gameplay/Collision/AscentGrip1"),true);
                    GD.Print($"TASK4_STEAM_REJOINS_MAIN_ASCENT_OK position={player.GlobalPosition}");
                }
                TestAssert.Equal(0,player.GetNode<PlayerMotor>("Motor").AcceptedDashCount,"gear connectors never need air dash");
            }
            catch(Exception error) { failures.Add(error); GD.Print($"TASK4_CONNECTOR_FAILURE lower={lower} error={error.Message}"); }
            finally {Task4FrameObserver=null;ReleaseAll();root.QueueFree();await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);await PhysicsFrames(tree,3);}
        }
        if(failures.Count != 0) throw new AggregateException("Actual gear connector failures", failures);
    }

    public static async Task RunTask4Ascent(SceneTree tree)
    {
        ActiveTree=tree; ReleaseAll();
        var root=new Node2D {Name="Task4MainAscent"};
        var basin=GD.Load<PackedScene>("res://scenes/prologue/sectors/CanyonBasin.tscn").Instantiate<PrologueSector>();
        var ascent=GD.Load<PackedScene>("res://scenes/prologue/sectors/CanyonAscent.tscn").Instantiate<PrologueSector>();
        var temple=GD.Load<PackedScene>("res://scenes/prologue/sectors/LostTemple.tscn").Instantiate<PrologueSector>();
        basin.Position=new(1280,3600); ascent.Position=new(3840,720); temple.Position=new(5120,720);
        var player=NewPlayer(); player.Position=new(3920,3548);
        player.GetNode<Camera2D>("Camera2D").Enabled=true;
        root.AddChild(basin); root.AddChild(ascent); root.AddChild(temple); root.AddChild(player); tree.Root.AddChild(root); ActiveInputCanvas=player;
        try
        {
            await WaitForFloor(tree,player);
            TestAssert.True(Task4HasFloorContact(player,ascent.GetNode<StaticBody2D>("Gameplay/Collision/LiftEntryFloor")),
                "main ascent starts on real CounterweightLift support");
            await Task4ReachSupport(tree,player,basin.GetNode<StaticBody2D>("Gameplay/Collision/ShortcutStep3636"),true);
            await Task4WalkTo(tree,player,4276,70);
            foreach(string name in new[]{"LiftAccessStepOne","LiftAccessStepTwo","LiftBackLanding"})
                await Task4ReachSupport(tree,player,ascent.GetNode<StaticBody2D>("Gameplay/Collision/"+name),false);
            for(int i=1;i<=9;i++)
            {
                await Task4ReachSupport(tree,player,ascent.GetNode<StaticBody2D>($"Gameplay/Collision/AscentGrip{i}"),true);
                GD.Print($"TASK4_MAIN_ASCENT_SUPPORT_OK index={i} position={player.GlobalPosition}");
            }
            await Task4ReachSupport(tree,player,ascent.GetNode<StaticBody2D>("Gameplay/Collision/AscentFinalCatch"),true);
            await Task4ReachSupport(tree,player,ascent.GetNode<StaticBody2D>("Gameplay/Collision/CourtyardApproach"),false);
            await Task4WalkTo(tree,player,5220,100);
            await Task4WaitSupport(tree,player,temple,"CourtyardFloor");
            TestAssert.Equal(0,player.GetNode<PlayerMotor>("Motor").AcceptedDashCount,"main ascent uses no unearned air dash");
            GD.Print($"TASK4_LIFT_PENDULUM_SPLIT_COURTYARD_INPUT_OK position={player.GlobalPosition}");
        }
        finally { ReleaseAll();root.QueueFree();await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);await PhysicsFrames(tree,3); }
    }

    // A controlled saved-geometry entry, not a substitute for the normal Explore
    // route. The pre-tree pose/velocity reproduce its coplanar Step8/Top handoff;
    // after mounting only the existing keyboard consumer may move the real body.
    public static async Task RunSteamTopSeamOnly(SceneTree tree)
    {
        ActiveTree = tree; ReleaseAll();
        var root = new RangedFairnessRecoveryProbe { Name = "SteamTopCoplanarEntry" };
        KeyboardRouteInputDriver keyboard = null;
        BasinBoardingTickObserver observer = null;
        Action previousObserver = Task4FrameObserver;
        bool checking = false, completedFloor = false, completedStep8 = false, completedTop = false;
        int completedTicks = 0, firstJumpCount = 0, firstDashCount = 0;
        string observationFault = null;
        Vector2 completedPosition = default, completedVelocity = default;
        try
        {
            PrologueSector basin = null;
            foreach (var id in new[] { PrologueSectorId.CanyonBasin, PrologueSectorId.CanyonAscent })
            {
                var sector = GD.Load<PackedScene>($"res://scenes/prologue/sectors/{id}.tscn")
                    .Instantiate<PrologueSector>();
                sector.Position = CanyonWorldLayout.SectorBounds[id].Position;
                root.AddChild(sector);
                foreach (KillZone zone in ShieldLaneNodes(sector).OfType<KillZone>())
                    zone.RoomControllerPath = zone.GetPathTo(root);
                if (id == PrologueSectorId.CanyonBasin) basin = sector;
            }
            var source = basin.GetNode<StaticBody2D>("Gameplay/Collision/SteamRejoinStep8");
            var target = basin.GetNode<StaticBody2D>("Gameplay/Collision/SteamRejoinTop");
            CharacterBody2D player = NewPlayer();
            player.Position = new Vector2(5009.1777f, 3563.925f);
            player.Velocity = new Vector2(170f, 0f); // Only before tree entry.
            root.AddChild(player); ActiveInputCanvas = player;
            var motor = player.GetNode<PlayerMotor>("Motor");
            var health = player.GetNode<HealthComponent>("Health");
            observer = new BasinBoardingTickObserver { ProcessPhysicsPriority = 100 };
            observer.Sample = _ =>
            {
                try
                {
                    completedPosition = player.GlobalPosition; completedVelocity = player.Velocity;
                    completedFloor = player.IsOnFloor();
                    completedStep8 = Task4HasFloorContact(player, source);
                    completedTop = Task4HasFloorContact(player, target);
                    if (checking) completedTicks++;
                }
                catch (Exception error) { observationFault ??= error.ToString(); }
            };
            player.AddChild(observer);
            tree.Root.AddChild(root);
            keyboard = KeyboardRouteInputDriver.Begin(player);
            for (int setup = 0; setup < 8 && !completedFloor; setup++) await PhysicsFrames(tree, 1);
            TestAssert.True(observationFault == null && completedFloor && completedStep8 && !completedTop &&
                completedPosition.X >= 4960f && completedPosition.X <= 5100f &&
                Mathf.Abs(completedPosition.Y + 36f - 3600f) <= 1f && completedVelocity.X > 0f,
                $"STEAM_TOP_SEAM_SETUP_REQUIRES_REAL_STEP8: floor={completedFloor} " +
                $"Step8={completedStep8} Top={completedTop} position={completedPosition} " +
                $"velocity={completedVelocity} fault={observationFault}");
            firstJumpCount = motor.AcceptedJumpCount; firstDashCount = motor.AcceptedDashCount;
            int hp = health.CurrentHealth;
            ulong cast = player.GetNode<MechanicalClawController>("MechanicalClaw").CurrentFrame.NextCastId;
            ulong startPhysics = Engine.GetPhysicsFrames();
            checking = true;
            Task4FrameObserver = () =>
            {
                previousObserver?.Invoke(); keyboard.ObserveFrame();
                TestAssert.True(observationFault == null && motor.AcceptedJumpCount == firstJumpCount,
                    $"STEAM_TOP_COPLANAR_EXTRA_JUMP_RED: same-height supported transfer must not jump; " +
                    $"physics={Engine.GetPhysicsFrames()} completed={completedTicks} " +
                    $"jumps={firstJumpCount}->{motor.AcceptedJumpCount} " +
                    $"position={completedPosition} velocity={completedVelocity} fault={observationFault}");
                TestAssert.True(completedFloor && health.CurrentHealth == hp && hp > 0 &&
                    root.RecoveryRequests == 0 && motor.AcceptedDashCount == firstDashCount,
                    "coplanar transfer keeps real support, HP and jet count without recovery");
            };
            GD.Print($"STEAM_TOP_SEAM_ENTRY physics={startPhysics} position={completedPosition} " +
                $"velocity={completedVelocity} cached_step8={completedStep8} cached_top={completedTop} " +
                "pre_tree_control=true full_route=false live_pose_writes=0");
            await Task4ReachSupport(tree, player, target, clawRequired: false);
            TestAssert.True(completedFloor && completedTop && completedPosition.X >= 4960f &&
                completedPosition.X <= 5100f && Mathf.Abs(completedPosition.Y + 36f - 3600f) <= 1f &&
                completedVelocity.Length() <= 2f && completedTicks > 0 && completedTicks <= 300 &&
                Engine.GetPhysicsFrames() - startPhysics <= 300UL && health.CurrentHealth == hp &&
                motor.AcceptedJumpCount == firstJumpCount && motor.AcceptedDashCount == firstDashCount &&
                player.GetNode<MechanicalClawController>("MechanicalClaw").CurrentFrame.NextCastId == cast &&
                root.RecoveryRequests == 0,
                $"STEAM_TOP_SEAM_NATIVE_RED: exact Top must physically support the stopped full capsule " +
                $"within original300 without jump/jet/claw/rescue; position={completedPosition} " +
                $"velocity={completedVelocity} exact={completedTop} ticks={completedTicks}");
            GD.Print($"THRUSTER_STEAM_TOP_SEAM_OK saved_two_sectors=true real_step8_entry=true " +
                $"native_keyboard=true exact_top_contact=true full_capsule=true original_budget_300=true " +
                $"extra_jump=false recovery_zero=true pre_tree_control=true full_route=false human=false " +
                $"completed_ticks={completedTicks} final={completedPosition} velocity={completedVelocity} hp={hp}");
        }
        finally
        {
            checking = false; Task4FrameObserver = previousObserver;
            if (observer != null) observer.Sample = null;
            keyboard?.Dispose(); ReleaseAll(); ActiveInputCanvas = null;
            if (GodotObject.IsInstanceValid(root)) root.Free();
        }
    }

    internal static async Task Task4ReachSupport(SceneTree tree, CharacterBody2D player, StaticBody2D support, bool clawRequired,
        bool keyboardShortcutSideBoost = false)
    {
        var shape = (RectangleShape2D)support.GetNode<CollisionShape2D>("Collision").Shape;
        Vector2 center = support.GlobalPosition;
        float top = center.Y-shape.Size.Y/2;
        int direction = center.X>player.GlobalPosition.X ? 1 : -1;
        var claw = player.GetNode<MechanicalClawController>("MechanicalClaw");
        var gripPost = support.GetParent().GetNodeOrNull<StaticBody2D>(support.Name+"Grip");
        var castPost = keyboardShortcutSideBoost ? null : gripPost;
        var sideInput = player.GetNode<PlayerInputReader>("Input");
        var sideMotor = player.GetNode<PlayerMotor>("Motor");
        var sideHealth = player.GetNode<HealthComponent>("Health");
        bool steamTopTrace = KeyboardRouteInputDriver.Current != null && !clawRequired && support.Name == "SteamRejoinTop";
        var steamTopRows = steamTopTrace ? new List<string>(64) : null;
        int steamTopSamples = 0, steamTopOmitted = 0, steamTopFaults = 0;
        bool steamTopReleaseObserved = false;
        bool steamTopPreviousFloor = player.IsOnFloor(), steamTopPreviousExact = false;
        BasinBoardingTickObserver steamTopObserver = null;
        bool FullSteamTop()
        {
            var collision = player.GetNode<CollisionShape2D>("BodyCollider");
            var capsule = (CapsuleShape2D)collision.Shape;
            Vector2 half = new(capsule.Radius, capsule.Height / 2f);
            Rect2 body = new Rect2(collision.ToGlobal(-half), collision.ToGlobal(half) - collision.ToGlobal(-half)).Abs();
            return player.IsOnFloor() && Task4HasFloorContact(player, support) &&
                body.Position.X >= center.X - shape.Size.X / 2f &&
                body.End.X <= center.X + shape.Size.X / 2f && Mathf.Abs(body.End.Y - top) <= 1f;
        }
        void SteamTopSnapshot(string edge, string timing)
        {
            if (!steamTopTrace) return;
            try
            {
                if (steamTopRows.Count >= 64) { steamTopOmitted++; return; }
                var contacts = new List<string>();
                for (int index = 0; index < Math.Min(player.GetSlideCollisionCount(), 8); index++)
                {
                    using var contact = player.GetSlideCollision(index);
                    contacts.Add($"{(contact.GetCollider() as Node)?.GetPath()}:{contact.GetNormal()}");
                }
                var bodyCollision = player.GetNode<CollisionShape2D>("BodyCollider");
                var capsule = (CapsuleShape2D)bodyCollision.Shape;
                Vector2 half = new(capsule.Radius, capsule.Height / 2f);
                Rect2 bounds = new Rect2(bodyCollision.ToGlobal(-half), bodyCollision.ToGlobal(half) - bodyCollision.ToGlobal(-half)).Abs();
                steamTopRows.Add($"TASK4_STEAM_TOP_TRACE row={steamTopRows.Count + 1}/64 edge={edge} timing={timing} " +
                    $"physics={Engine.GetPhysicsFrames()} sample={sideInput.CurrentFrameSequence} player={player.GlobalPosition} " +
                    $"velocity={player.Velocity} body={bounds} target={support.GetPath()}#{support.GetInstanceId()} " +
                    $"target_center={center} target_size={shape.Size} direction={direction} " +
                    $"floor={player.IsOnFloor()} exact_contact={Task4HasFloorContact(player, support)} " +
                    $"rawAD={Input.IsPhysicalKeyPressed(Key.A)}/{Input.IsPhysicalKeyPressed(Key.D)} " +
                    $"rawSpace={Input.IsPhysicalKeyPressed(Key.Space)} sampled_axis={sideInput.CurrentFrame.MoveAxis} " +
                    $"jump={sideInput.CurrentFrame.JumpPressed}/{sideInput.CurrentFrame.JumpHeld} " +
                    $"accepted_jumps={sideMotor.AcceptedJumpCount} dashes={sideMotor.AcceptedDashCount} " +
                    $"hp={sideHealth.CurrentHealth} landing={sideMotor.LandingCount} contacts=[{string.Join(";", contacts)}] observer_only=true");
            }
            catch (Exception) { steamTopFaults++; }
        }
        // Retain the transient release reason which is otherwise reset to Idle
        // before the original 300-frame support assertion. Diagnostic only:
        // no resolver, TestMove, input write or extra frame is introduced.
        bool grip4Trace = support.Name.ToString() is "AscentGrip2" or "AscentGrip4" or "AscentGrip5" or "AscentGrip6" or
            "AscentGrip7" or "AscentGrip8" or "AscentGrip9";
        var grip4Rows = grip4Trace ? new List<string>(64) : null;
        MechanicalClawState? grip4LastState = null;
        ulong grip4LastPhysics = 0;
        int grip4Omitted = 0, grip4ObservationErrors = 0;
        void ObserveGrip4(MechanicalClawFrame frame, GrappleSurface2D target)
        {
            try
            {
                ulong physics = Engine.GetPhysicsFrames();
                bool transition = grip4LastState != frame.State;
                grip4LastState = frame.State;
                bool activeSample = frame.State is MechanicalClawState.Travel or MechanicalClawState.Latched or
                    MechanicalClawState.Reel or MechanicalClawState.Swing;
                if (!transition && (!activeSample || physics - grip4LastPhysics < 4)) return;
                if (grip4Rows.Count >= (transition ? 64 : 48)) { grip4Omitted++; return; }
                grip4LastPhysics = physics;
                var contacts = new List<string>(4);
                for (int index = 0; index < Math.Min(player.GetSlideCollisionCount(), 4); index++)
                {
                    using var contact = player.GetSlideCollision(index);
                    contacts.Add($"{(contact.GetCollider() as Node)?.GetPath()}:{contact.GetNormal()}");
                }
                grip4Rows.Add($"TASK4_ASCENT_PASSIVE support={support.Name} physics={physics} sample={sideInput.CurrentFrameSequence} " +
                    $"state={frame.State} failure={frame.Failure} release={frame.ReleaseKind} cast={frame.CastId} " +
                    $"player={player.GlobalPosition} velocity={player.Velocity} correction={frame.VelocityCorrection} " +
                    $"caster_velocity={frame.CasterVelocity} anchor={frame.AnchorPoint} dock={frame.DockPoint} " +
                    $"arrived={frame.Arrived} target={target?.GetPath()} locked={frame.LockedTargetRuntimeId} " +
                    $"move={sideInput.CurrentFrame.MoveAxis} aim={sideInput.KeyboardAimDirection} " +
                    $"K={sideInput.CurrentFrame.ClawPressed} jump={sideInput.CurrentFrame.JumpPressed} " +
                    $"hp={sideHealth.CurrentHealth} floor={player.IsOnFloor()} contacts=[{string.Join(";", contacts)}]");
            }
            catch (Exception) { grip4ObservationErrors++; }
        }
        var sideSurface = keyboardShortcutSideBoost ? support.GetNode<GrappleSurface2D>("GrappleSurfaceProfile") : null;
        ulong sideStart = Engine.GetPhysicsFrames(), sideCast = claw.CurrentFrame.NextCastId;
        ulong sideSequence = sideInput.CurrentFrameSequence, sideReleaseSequence = 0UL;
        int sideJumps = sideMotor.AcceptedJumpCount, sideDashes = sideMotor.AcceptedDashCount;
        int sideHp = sideHealth.CurrentHealth, sideEdges = KeyboardRouteInputDriver.Current?.ClawEdges ?? 0;
        bool ascentWalkStarted = false, ascentWalkPending = false;
        float ascentClearX = float.NaN;
        StaticBody2D ascentSource = null;
        CollisionShape2D ascentBody = null;
        StringName ascentWalkKey = direction < 0 ? InputActions.MoveLeft : InputActions.MoveRight;
        int sideKSamples = 0;
        bool sideAttached = false, sideArrived = false, sideJumpReleased = false;
        bool sideNativeArrivalSeen = false;
        bool sideBoostIssued = false, sideBoostPublished = false, sideBoostReceipt = false, sideInvalidReceipt = false;
        ulong sideBoostPhysics = 0UL, sideBoostSequence = 0UL;
        ulong sideBoostRisePhysics = 0UL;
        int sideBoostRiseSamples = 0;
        float sideBoostY = float.NaN;
        Vector2 sideBodyHalf=Vector2.Zero;
        Func<MechanicalClawTargetResolution,bool> sideRaySafe=null;
        void FullSideTarget()
        {
            Vector2 bodyCenter=player.GetNode<CollisionShape2D>("BodyCollider").GlobalPosition;
            TestAssert.True(player.IsOnFloor() && Task4HasFloorContact(player,support) &&
                bodyCenter.X-sideBodyHalf.X>=center.X-shape.Size.X/2 &&
                bodyCenter.X+sideBodyHalf.X<=center.X+shape.Size.X/2 &&
                Mathf.Abs(bodyCenter.Y+sideBodyHalf.Y-top)<=1,
                "side/Boost retains full actual capsule bounds on the original target platform");
            foreach(float footX in new[]{bodyCenter.X,bodyCenter.X-sideBodyHalf.X+1,bodyCenter.X+sideBodyHalf.X-1})
            {
                using var query=PhysicsRayQueryParameters2D.Create(new(footX,top-.25f),new(footX,top+1),CombatCollisionLayers.World);
                query.CollideWithBodies=true; query.CollideWithAreas=false;
                using var hit=player.GetWorld2D().DirectSpaceState.IntersectRay(query);
                TestAssert.True(hit.Count>0 && hit["collider"].AsGodotObject()==support &&
                    hit["normal"].AsVector2().Y<-.99f,
                    "side/Boost target center and both capsule edges retain the same original native support");
            }
        }
        void SideGuard()
        {
            if (!keyboardShortcutSideBoost) return;
            TestAssert.True(Engine.GetPhysicsFrames()-sideStart<=300UL && player.IsInsideTree() &&
                support.IsInsideTree() && !tree.Paused && player.GlobalPosition.IsFinite() && player.Velocity.IsFinite() &&
                sideHealth.CurrentHealth==sideHp && sideHp>0 && sideMotor.AcceptedDashCount==sideDashes &&
                sideMotor.AcceptedJumpCount>=sideJumps && sideMotor.AcceptedJumpCount<=sideJumps+1 &&
                claw.CurrentFrame.NextCastId>=sideCast && claw.CurrentFrame.NextCastId<=sideCast+1UL &&
                !sideInvalidReceipt && sideKSamples<=1 &&
                KeyboardRouteInputDriver.Current.ClawEdges>=sideEdges &&
                KeyboardRouteInputDriver.Current.ClawEdges<=sideEdges+1 &&
                sideInput.CurrentFrame.KeyboardActions &&
                !sideInput.CurrentFrame.PrimaryPressed && !sideInput.CurrentFrame.PrimaryHeld &&
                !sideInput.CurrentFrame.PrimaryReleased && !sideInput.CurrentFrame.GrappleModePressed &&
                !sideInput.CurrentFrame.AttackModePressed && !sideInput.CurrentFrame.AttackPressed &&
                !sideInput.CurrentFrame.InteractPressed && !sideInput.CurrentFrame.ShieldHeld &&
                !sideInput.CurrentFrame.LegacyShieldHeld && !sideInput.CurrentFrame.DashPressed,
                $"keyboard shortcut side/Boost keeps original300/player/HP/one ordinary jump/one cast/source: " +
                $"support={support.Name} physics={Engine.GetPhysicsFrames()-sideStart}/300 " +
                $"jump={sideJumps}->{sideMotor.AcceptedJumpCount} K={sideKSamples} " +
                $"attached={sideAttached} arrived={sideArrived} boost={sideBoostReceipt} invalid={sideInvalidReceipt}");
        }
        void ObserveSideFrame(MechanicalClawFrame frame, GrappleSurface2D target)
        {
            if(sideInput.CurrentFrameSequence>sideSequence)
            {
                sideSequence=sideInput.CurrentFrameSequence;
                if(sideInput.CurrentFrame.ClawPressed) sideKSamples++;
            }
            if(frame.Failure!=MechanicalClawFailure.None || frame.CastId!=0UL && frame.CastId!=sideCast)
                sideInvalidReceipt=true;
            if(frame.State is MechanicalClawState.Latched or MechanicalClawState.Reel or MechanicalClawState.Swing)
            {
                bool exact=target==sideSurface && frame.CastId==sideCast &&
                    frame.LockedTargetRuntimeId==sideSurface.GetInstanceId();
                sideInvalidReceipt|=!exact;
                sideAttached|=exact;
                sideArrived|=exact && frame.Arrived;
            }
            if(frame.State is MechanicalClawState.Boost or MechanicalClawState.Released)
            {
                GD.Print($"TASK4_KEYBOARD_SIDE_RELEASE_FACTS support={support.Name} physics={Engine.GetPhysicsFrames()} " +
                    $"state={frame.State} release={frame.ReleaseKind} consumed={frame.BoostConsumed} arrived={frame.Arrived} " +
                    $"issued={sideBoostIssued} attached_seen={sideAttached} arrived_seen={sideArrived} jump_released={sideJumpReleased} " +
                    $"cast={frame.CastId}/{sideCast} next={frame.NextCastId} runtime={frame.LockedTargetRuntimeId}/{sideSurface.GetInstanceId()} " +
                    $"target={target?.GetPath()} expected_target={sideSurface.GetPath()} sequence={sideInput.CurrentFrameSequence}/{sideReleaseSequence} " +
                    $"jump_pressed={sideInput.CurrentFrame.JumpPressed} jump_held={sideInput.CurrentFrame.JumpHeld} " +
                    $"axis={sideInput.CurrentFrame.MoveAxis} direction={direction} physical_space={Input.IsPhysicalKeyPressed(Key.Space)} " +
                    $"ordinary_jumps={sideJumps}->{sideMotor.AcceptedJumpCount} published={sideBoostPublished} receipt={sideBoostReceipt}");
                bool exact=sideBoostIssued && sideAttached && sideArrived && sideNativeArrivalSeen && sideJumpReleased &&
                    // Swing can leave the eight-pixel dock tolerance before the
                    // fresh Space edge is sampled. The same-cast arrival was
                    // independently witnessed before issuing this Boost.
                    frame.ReleaseKind==MechanicalClawReleaseKind.Boost && frame.BoostConsumed &&
                    frame.CastId==sideCast && frame.LockedTargetRuntimeId==sideSurface.GetInstanceId() &&
                    frame.NextCastId==sideCast+1UL && sideInput.CurrentFrameSequence>sideReleaseSequence &&
                    sideInput.CurrentFrame.JumpPressed && sideInput.CurrentFrame.JumpHeld &&
                    sideInput.CurrentFrame.MoveAxis*direction<-.99f &&
                    Input.IsPhysicalKeyPressed(Key.Space) && sideMotor.AcceptedJumpCount==sideJumps+1;
                if(frame.State==MechanicalClawState.Boost)
                {
                    exact&=target==sideSurface && !sideBoostPublished && !sideBoostReceipt;
                    sideBoostPublished|=exact;
                    sideBoostPhysics=Engine.GetPhysicsFrames(); sideBoostSequence=sideInput.CurrentFrameSequence;
                    sideBoostRisePhysics=sideBoostPhysics; sideBoostY=player.GlobalPosition.Y;
                }
                else
                {
                    exact&=sideBoostPublished && !sideBoostReceipt && target==null &&
                        Engine.GetPhysicsFrames()==sideBoostPhysics && sideInput.CurrentFrameSequence==sideBoostSequence;
                    sideBoostReceipt|=exact;
                }
                sideInvalidReceipt|=!exact;
            }
        }
        if(keyboardShortcutSideBoost)
        {
            TestAssert.True(KeyboardRouteInputDriver.Current!=null && clawRequired && gripPost!=null &&
                support.Name.ToString() is "ShortcutStep3796" or "ShortcutStep3636" or "ShortcutStep2996" or "ShortcutStep2676" or "ShortcutStep2036" or "ShortcutStep1716",
                "explicit keyboard side/Boost applies only to the six original post/corner conflicts");
            claw.ClawFrameChanged+=ObserveSideFrame;
        }
        if (grip4Trace) claw.ClawFrameChanged += ObserveGrip4;
        try
        {
        if (steamTopTrace)
        {
            steamTopObserver = new BasinBoardingTickObserver { ProcessPhysicsPriority = 100 };
            steamTopObserver.Sample = _ =>
            {
                try
                {
                    steamTopSamples++;
                    bool onFloor = player.IsOnFloor(), exact = Task4HasFloorContact(player, support);
                    if (steamTopSamples <= 8 || steamTopSamples % 20 == 0 ||
                        onFloor != steamTopPreviousFloor || exact != steamTopPreviousExact)
                        SteamTopSnapshot("completed-step", "post-Motor-priority100");
                    steamTopPreviousFloor = onFloor; steamTopPreviousExact = exact;
                }
                catch (Exception) { steamTopFaults++; }
            };
            player.AddChild(steamTopObserver);
            SteamTopSnapshot("entry-before-original-release", "callsite-pre-node");
        }
        bool verboseTrace = Array.Exists(OS.GetCmdlineUserArgs(), value =>
            value == "--task4-support-trace");
        bool ascentGrip8Diagnostic = support.Name == "AscentGrip8" &&
            Array.Exists(OS.GetCmdlineUserArgs(), value => value == "--diagnose-ascent-grip8");
        bool shortcut2996Observation = support.Name == "ShortcutStep2996";
        int shortcut2996Samples = 0;
        int shortcut2996LastLocalFrame = -1;
        if(shortcut2996Observation)
            Task23ObserveShortcut2996(player,support,claw,center,shape,clawRequired,
                direction,"entry-before-release",-1,false,ref shortcut2996Samples);
        ReleaseAll();
        // The saved Step8 is wholly inside Top at the same height. Native floor
        // caches can legitimately keep Step8 after that transfer; this is not a
        // reason to manufacture a jump. Enter only with real Step8 support, then
        // walk to Top's exclusive strip and still demand the original exact Top
        // contact. All control and settling share the original 300-tick cap.
        var coplanarSource = steamTopTrace && !FullSteamTop()
            ? support.GetParent().GetNodeOrNull<StaticBody2D>("SteamRejoinStep8") : null;
        if (coplanarSource != null && player.IsOnFloor() && Task4HasFloorContact(player, coplanarSource) &&
            claw.CurrentFrame.State == MechanicalClawState.Idle && !sideMotor.IsDashing)
        {
            var sourceCollision = coplanarSource.GetNode<CollisionShape2D>("Collision");
            var targetCollision = support.GetNode<CollisionShape2D>("Collision");
            var bodyCollision = player.GetNode<CollisionShape2D>("BodyCollider");
            TestAssert.True(sourceCollision is { Disabled: false, Shape: RectangleShape2D } &&
                targetCollision is { Disabled: false, Shape: RectangleShape2D } &&
                bodyCollision is { Disabled: false, Shape: CapsuleShape2D } &&
                sourceCollision.GlobalTransform.X.IsEqualApprox(Vector2.Right) &&
                sourceCollision.GlobalTransform.Y.IsEqualApprox(Vector2.Down) &&
                targetCollision.GlobalTransform.X.IsEqualApprox(Vector2.Right) &&
                targetCollision.GlobalTransform.Y.IsEqualApprox(Vector2.Down) &&
                bodyCollision.GlobalTransform.X.IsEqualApprox(Vector2.Right) &&
                bodyCollision.GlobalTransform.Y.IsEqualApprox(Vector2.Down),
                "coplanar Top handoff uses unchanged enabled translation-only saved rectangles and capsule");
            var sourceShape = (RectangleShape2D)sourceCollision.Shape;
            var capsule = (CapsuleShape2D)bodyCollision.Shape;
            Rect2 sourceBounds = new(sourceCollision.GlobalPosition - sourceShape.Size / 2f, sourceShape.Size);
            Rect2 targetBounds = new(targetCollision.GlobalPosition - shape.Size / 2f, shape.Size);
            float safeLeft = targetBounds.Position.X + capsule.Radius + player.SafeMargin;
            float exclusiveRight = sourceBounds.Position.X - player.SafeMargin;
            float stanceX = (safeLeft + exclusiveRight) * .5f;
            bool FullCoplanarSupport() => player.IsOnFloor() &&
                (Task4HasFloorContact(player, coplanarSource) || Task4HasFloorContact(player, support)) &&
                bodyCollision.GlobalPosition.X - capsule.Radius >= targetBounds.Position.X &&
                bodyCollision.GlobalPosition.X + capsule.Radius <= targetBounds.End.X &&
                Mathf.Abs(bodyCollision.GlobalPosition.Y + capsule.Height / 2f - targetBounds.Position.Y) <= 1f;
            TestAssert.True(Mathf.Abs(sourceBounds.Position.Y - targetBounds.Position.Y) < .001f &&
                sourceBounds.Position.X >= targetBounds.Position.X && sourceBounds.End.X <= targetBounds.End.X &&
                safeLeft < exclusiveRight && FullCoplanarSupport(),
                "coplanar Step8 is inside Top and its actual full capsule can walk into a distinct safe Top strip");
            int hp = sideHealth.CurrentHealth, jumps = sideMotor.AcceptedJumpCount,
                dashes = sideMotor.AcceptedDashCount, waits = 0;
            ulong nextCast = claw.CurrentFrame.NextCastId, startPhysics = Engine.GetPhysicsFrames(),
                startSequence = sideInput.CurrentFrameSequence;
            bool settled = false;
            double delta = 1d / Engine.PhysicsTicksPerSecond;
            // At most one eighth of the narrow center interval per completed
            // step. This is an input planner speed bound, not a body speed write.
            float speedCap = (exclusiveRight - safeLeft) / (8f * (float)delta);
            while (waits < 300 && !settled)
            {
                TestAssert.True(FullCoplanarSupport() && hp > 0 && sideHealth.CurrentHealth == hp &&
                    sideMotor.AcceptedJumpCount == jumps && sideMotor.AcceptedDashCount == dashes &&
                    claw.CurrentFrame.State == MechanicalClawState.Idle && claw.CurrentFrame.NextCastId == nextCast,
                    "coplanar Top walk preserves physical support, HP and all action counts");
                float vx = player.Velocity.X;
                var neutral = FreeMotionForecast.Neutral(vx, true, sideMotor.MotionSettings,
                    sideMotor.ThrusterState.Module, delta, 300 - waits, 2f);
                var left = ThrusterDynamics.Step(new(new Vector2(vx, 0f), -1f, true,
                    Vector2.Zero, 0f, ApplyGravity: false), sideMotor.ThrusterState.Module,
                    sideMotor.MotionSettings, delta);
                bool pressLeft = vx > 2f || player.GlobalPosition.X + neutral.NetDistance > stanceX + .5f &&
                    Mathf.Abs(left.Velocity.X) <= speedCap;
                SendAction(InputActions.MoveLeft, pressLeft);
                SendAction(InputActions.MoveRight, false);
                await PhysicsFrames(tree, 1); waits++;
                settled = FullSteamTop() && player.Velocity.Length() <= 2f &&
                    sideInput.CurrentFrameSequence > startSequence && sideInput.CurrentFrame.MoveAxis == 0f &&
                    !sideInput.CurrentFrame.JumpPressed && !sideInput.CurrentFrame.JumpHeld &&
                    !Input.IsPhysicalKeyPressed(Key.A) && !Input.IsPhysicalKeyPressed(Key.D) &&
                    !Input.IsPhysicalKeyPressed(Key.Space);
            }
            SendAction(InputActions.MoveLeft, false); SendAction(InputActions.MoveRight, false);
            TestAssert.True(settled && FullSteamTop() && waits <= 300 &&
                Engine.GetPhysicsFrames() - startPhysics <= 300UL && sideHealth.CurrentHealth == hp &&
                sideMotor.AcceptedJumpCount == jumps && sideMotor.AcceptedDashCount == dashes &&
                claw.CurrentFrame.NextCastId == nextCast,
                $"coplanar ground transfer retains original exact Top/full support and settles within300; " +
                $"position={player.GlobalPosition} velocity={player.Velocity} waits={waits}");
            SteamTopSnapshot("coplanar-ground-transfer-complete", "callsite-after-completed-cache");
            GD.Print($"TASK4_STEAM_TOP_COPLANAR_OK waits={waits}/300 physics={Engine.GetPhysicsFrames()} " +
                $"stance_x={stanceX:R} speed_cap={speedCap:R} player={player.GlobalPosition} velocity={player.Velocity} " +
                $"hp={hp}->{sideHealth.CurrentHealth} jumps={jumps}->{sideMotor.AcceptedJumpCount} " +
                $"dashes={dashes}->{sideMotor.AcceptedDashCount} next_cast={nextCast}->{claw.CurrentFrame.NextCastId} " +
                "exact_top_contact=true full_capsule=true extra_jump=false ground_transfer_only=true");
            return;
        }
        // Step8 and Top overlap at the same saved height. A real completed
        // Step8 tail can already be fully on the exact Top collider; forcing
        // another departure here invents a jump that the route does not need.
        if (steamTopTrace && FullSteamTop() && claw.CurrentFrame.State == MechanicalClawState.Idle)
        {
            var neutral = FreeMotionForecast.Neutral(player.Velocity.X, true, sideMotor.MotionSettings,
                sideMotor.ThrusterState.Module, 1d / Engine.PhysicsTicksPerSecond, 300, 2f);
            float finalX = player.GlobalPosition.X + neutral.NetDistance;
            var capsule = (CapsuleShape2D)player.GetNode<CollisionShape2D>("BodyCollider").Shape;
            if (neutral.SpeedThresholdReached &&
                finalX - capsule.Radius >= center.X - shape.Size.X / 2f &&
                finalX + capsule.Radius <= center.X + shape.Size.X / 2f)
            {
                int entryHp = sideHealth.CurrentHealth, entryJumps = sideMotor.AcceptedJumpCount,
                    entryDashes = sideMotor.AcceptedDashCount;
                ulong entryCast = claw.CurrentFrame.NextCastId;
                int neutralWaits = 0;
                bool stable = false;
                while (neutralWaits < 300 && !stable)
                {
                    await PhysicsFrames(tree, 1); neutralWaits++;
                    TestAssert.True(FullSteamTop() && sideHealth.CurrentHealth == entryHp && entryHp > 0 &&
                        sideMotor.AcceptedJumpCount == entryJumps && sideMotor.AcceptedDashCount == entryDashes &&
                        claw.CurrentFrame.State == MechanicalClawState.Idle && claw.CurrentFrame.NextCastId == entryCast,
                        "already-arrived Steam Top retains full exact support and original HP/actions throughout neutral settling");
                    stable = Mathf.Abs(player.Velocity.X) <= 2f && Mathf.Abs(player.Velocity.Y) <= 2f &&
                        sideInput.CurrentFrame.MoveAxis == 0f && !sideInput.CurrentFrame.JumpPressed &&
                        !sideInput.CurrentFrame.JumpHeld && !Input.IsPhysicalKeyPressed(Key.A) &&
                        !Input.IsPhysicalKeyPressed(Key.D) && !Input.IsPhysicalKeyPressed(Key.Space);
                }
                TestAssert.True(stable, "already-arrived Steam Top settles inside the original300 cap");
                SteamTopSnapshot("already-arrived-neutral-complete", "callsite-pre-node");
                GD.Print($"TASK4_STEAM_TOP_ALREADY_SUPPORTED_OK physics={Engine.GetPhysicsFrames()} " +
                    $"frames={neutralWaits}/300 player={player.GlobalPosition} velocity={player.Velocity} " +
                    $"predicted_neutral_x={finalX:R} hp={entryHp}->{sideHealth.CurrentHealth} " +
                    $"jumps={entryJumps}->{sideMotor.AcceptedJumpCount} dashes={entryDashes}->{sideMotor.AcceptedDashCount} " +
                    $"next_cast={entryCast}->{claw.CurrentFrame.NextCastId} exact_native_support=true extra_jump=false");
                return;
            }
        }
        bool releaseIssued=false, boostApplied=false, attached=false, departed=false, boostTurnPending=false;
        if(!clawRequired && top < player.GlobalPosition.Y+32)
        {
            float nearEdge=center.X-direction*shape.Size.X/2;
            float rise=player.GlobalPosition.Y+36-top;
            float approach=(nearEdge-player.GlobalPosition.X)*direction-(rise<=24 ? 35 : 90);
            if(approach>0)
            {
                int approachBudget=90;
                if(KeyboardRouteInputDriver.Current!=null && support.Name=="GearPistonTwo")
                {
                    var piston=support.GetParent().GetParent().GetNode<CanyonGearPiston>("Mechanisms/GearPiston");
                    // The new keyboard path reaches this authored timed blocker at
                    // a different phase. Admit at most one actual active interval,
                    // not a physics/HP bypass or an unbounded walking retry.
                    approachBudget+=Mathf.CeilToInt(piston.ActiveSeconds*Engine.PhysicsTicksPerSecond)+4;
                }
                await Task4WalkTo(tree,player,player.GlobalPosition.X+direction*approach,approachBudget);
            }
        }
        if (clawRequired)
        {
            SendAction(InputActions.SelectGrappleMode,true); await PhysicsFrames(tree,1); SendAction(InputActions.SelectGrappleMode,false);
            if(keyboardShortcutSideBoost)
            {
                var bodyCollision=player.GetNode<CollisionShape2D>("BodyCollider");
                TestAssert.True(bodyCollision.Shape is CapsuleShape2D && !bodyCollision.Disabled &&
                    Mathf.Abs(bodyCollision.GlobalTransform.X.Y)<.001f && Mathf.Abs(bodyCollision.GlobalTransform.Y.X)<.001f,
                    "keyboard side/Boost begins with a real enabled upright capsule");
                var capsule=(CapsuleShape2D)bodyCollision.Shape;
                Vector2 bodyHalf=new(capsule.Radius*Mathf.Abs(bodyCollision.GlobalTransform.X.X),
                    capsule.Height*.5f*Mathf.Abs(bodyCollision.GlobalTransform.Y.Y));
                sideBodyHalf=bodyHalf;
                int targetY=int.Parse(support.Name.ToString()["ShortcutStep".Length..]);
                StaticBody2D previous=Enumerable.Range(0,player.GetSlideCollisionCount())
                    .Select(index=>player.GetSlideCollision(index)?.GetCollider() as StaticBody2D)
                    .FirstOrDefault(body=>body?.Name.ToString()==$"ShortcutStep{targetY+160}");
                TestAssert.True(previous!=null && bodyHalf.IsFinite() && bodyHalf.X>0 && bodyHalf.Y>=bodyHalf.X &&
                    bodyCollision.GlobalPosition.DistanceTo(player.GlobalPosition)<.01f,
                    "keyboard side/Boost retains the exact authored previous step and full actual capsule");
                var previousCollision=previous.GetNode<CollisionShape2D>("Collision");
                TestAssert.True(previousCollision.Shape is RectangleShape2D && !previousCollision.Disabled &&
                    Mathf.Abs(previousCollision.GlobalTransform.X.Y)<.001f && Mathf.Abs(previousCollision.GlobalTransform.Y.X)<.001f,
                    "keyboard side/Boost source support is an enabled upright authored rectangle");
                var previousShape=(RectangleShape2D)previousCollision.Shape;
                float previousLeft=previousCollision.ToGlobal(new Vector2(-previousShape.Size.X/2,0)).X;
                float previousRight=previousCollision.ToGlobal(new Vector2(previousShape.Size.X/2,0)).X;
                float previousTop=previousCollision.ToGlobal(new Vector2(0,-previousShape.Size.Y/2)).Y;
                void FullSource()
                {
                    SideGuard();
                    Vector2 bodyCenter=bodyCollision.GlobalPosition;
                    TestAssert.True(player.IsOnFloor() && bodyCenter.X-bodyHalf.X>=previousLeft &&
                        bodyCenter.X+bodyHalf.X<=previousRight && Mathf.Abs(bodyCenter.Y+bodyHalf.Y-previousTop)<=1 &&
                        Mathf.Abs(player.Velocity.Y)<=.01f && sideMotor.AcceptedJumpCount==sideJumps &&
                        claw.CurrentFrame.State==MechanicalClawState.Idle && claw.CurrentTarget==null,
                        "side/Boost ordinary jump starts on the complete original support, before any K");
                    foreach(float footX in new[]{bodyCenter.X,bodyCenter.X-bodyHalf.X+1,bodyCenter.X+bodyHalf.X-1})
                    {
                        using var query=PhysicsRayQueryParameters2D.Create(new(footX,previousTop-.25f),new(footX,previousTop+1),CombatCollisionLayers.World);
                        query.CollideWithBodies=true; query.CollideWithAreas=false;
                        using var hit=player.GetWorld2D().DirectSpaceState.IntersectRay(query);
                        TestAssert.True(hit.Count>0 && hit["collider"].AsGodotObject()==previous &&
                            hit["normal"].AsVector2().Y<-.99f,
                            "side/Boost center and both capsule edges rest on the same original native step");
                    }
                }
                FullSource();
                for(int neutral=0;neutral<12 && (Mathf.Abs(player.Velocity.X)>2 || sideInput.CurrentFrame.MoveAxis!=0 ||
                    sideInput.CurrentFrame.JumpHeld || sideInput.CurrentFrame.JumpPressed);neutral++)
                { await PhysicsFrames(tree,1); FullSource(); }
                TestAssert.True(Mathf.Abs(player.Velocity.X)<=2 && sideInput.CurrentFrame.MoveAxis==0 &&
                    !sideInput.CurrentFrame.JumpHeld && !sideInput.CurrentFrame.JumpPressed &&
                    !Input.IsPhysicalKeyPressed(Key.Space),"side/Boost begins from stopped real released Space");
                var sideResolver=new MechanicalClawTargetResolver();
                Vector2 horizontal=new(direction,0);
                float near=center.X-direction*shape.Size.X/2;
                bool SafeSide(MechanicalClawTargetResolution candidate)
                {
                    if(candidate.Surface!=sideSurface || !candidate.Snapshot.Exists || !candidate.Snapshot.HasLineOfSight ||
                        candidate.Snapshot.IsLegacyInteraction || candidate.Snapshot.RuntimeId!=sideSurface.GetInstanceId() ||
                        candidate.Snapshot.Material!=GrappleSurfaceMaterial.GripStone ||
                        candidate.Snapshot.SurfaceNormal.Dot(-horizontal)<.999f ||
                        Mathf.Abs(candidate.Snapshot.WorldAnchor.X-near)>.01f ||
                        candidate.Snapshot.WorldAnchor.Y<top+4 || candidate.Snapshot.WorldAnchor.Y>top+shape.Size.Y-4 ||
                        player.GlobalPosition.DistanceTo(candidate.Snapshot.WorldAnchor)>620 ||
                        !player.GetViewportRect().HasPoint(player.GetCanvasTransform()*candidate.Snapshot.WorldAnchor)) return false;
                    using var ray=PhysicsRayQueryParameters2D.Create(player.GlobalPosition,
                        player.GlobalPosition+horizontal*620,CombatCollisionLayers.World|CombatCollisionLayers.ThreadTarget);
                    ray.CollideWithBodies=true; ray.CollideWithAreas=true;
                    using var hit=player.GetWorld2D().DirectSpaceState.IntersectRay(ray);
                    return hit.Count>0 && hit["collider"].AsGodotObject()==support &&
                        hit["normal"].AsVector2().Dot(-horizontal)>.999f &&
                        hit["position"].AsVector2().DistanceTo(candidate.Snapshot.WorldAnchor)<.01f;
                }
                sideRaySafe=SafeSide;
                SendAction(InputActions.Jump,true);
                bool ready=false;
                ulong riseStart=Engine.GetPhysicsFrames();
                int riseWaits=0;
                do
                {
                    SideGuard();
                    await tree.ToSignal(tree,SceneTree.SignalName.PhysicsFrame);
                    SideGuard();
                    ulong sameTick=Engine.GetPhysicsFrames();
                    await tree.ToSignal(tree.CreateTimer(0d,true,true),SceneTreeTimer.SignalName.Timeout);
                    TestAssert.Equal(sameTick,Engine.GetPhysicsFrames(),"side/Boost post-Motor rise snapshot consumes no hidden physics tick");
                    Task4FrameObserver?.Invoke(); SideGuard(); riseWaits++;
                    var candidate=sideResolver.ResolveKeyboard(player,horizontal);
                    ready=sideMotor.AcceptedJumpCount==sideJumps+1 && SafeSide(candidate);
                    if(ready || riseWaits==24)
                        GD.Print($"TASK4_KEYBOARD_SIDE_RISE support={support.Name} waits={riseWaits}/24 " +
                            $"physics={Engine.GetPhysicsFrames()-riseStart} player={player.GlobalPosition} velocity={player.Velocity} " +
                            $"selected={candidate.Surface?.GetPath()} anchor={candidate.Snapshot.WorldAnchor} " +
                            $"normal={candidate.Snapshot.SurfaceNormal} ready={ready}");
                } while(!ready && riseWaits<24 && Engine.GetPhysicsFrames()-riseStart<24UL);
                TestAssert.True(ready && Engine.GetPhysicsFrames()-riseStart<=24UL &&
                    sideInput.CurrentFrame.JumpHeld && !sideInput.CurrentFrame.JumpPressed &&
                    Input.IsPhysicalKeyPressed(Key.Space) && sideMotor.AcceptedJumpCount==sideJumps+1,
                    "one real ordinary jump admits the original native platform side, four pixels away from either corner, within24");
            }
            Vector2 aim = keyboardShortcutSideBoost ? new(center.X-direction*(shape.Size.X/2-2),player.GlobalPosition.Y) :
                castPost == null ? new(center.X-direction*(shape.Size.X/2-2),top+2) :
                new(castPost.GlobalPosition.X,castPost.GlobalPosition.Y-
                    ((RectangleShape2D)castPost.GetNode<CollisionShape2D>("Collision").Shape).Size.Y/2+10);
            TestAssert.True(player.GetViewportRect().HasPoint(player.GetCanvasTransform()*aim),
                $"{support.Name} real cast point must be on screen before input");
            var keyboardDriver=KeyboardRouteInputDriver.Current;
            var input=player.GetNode<PlayerInputReader>("Input");
            var resolver=new MechanicalClawTargetResolver();
            Vector2 plannedDirection=PlayerKeyboardAimModel.ResolveDirection(aim-player.GlobalPosition, direction);
            // This is only a planner preflight. The accepted K sample below must
            // independently prove the actual production direction and selection.
            var resolution=keyboardDriver!=null
                ? resolver.ResolveKeyboard(player,plannedDirection) : resolver.Resolve(player,aim);
            // The authored Grip3..9 platforms/posts are alternating mirrors. A
            // 45-degree keyboard ray from the near edge of the previous shelf
            // can hit the target's own platform or the previous shelf's post
            // before the intended above-shelf post (including after combat).
            // Prepare one analytically bounded stance; never search receivers,
            // force a target, or change production collision/aim rules.
            string ascentName=support.Name.ToString();
            int ascentIndex=0;
            bool mirroredAscent=ascentName.StartsWith("AscentGrip",StringComparison.Ordinal) &&
                int.TryParse(ascentName["AscentGrip".Length..],out ascentIndex) &&
                ascentIndex is >=3 and <=9;
            var previousAscent=keyboardDriver!=null && mirroredAscent
                ? support.GetParent().GetNodeOrNull<StaticBody2D>($"AscentGrip{ascentIndex-1}") : null;
            var previousGripPost=previousAscent?.GetParent().GetNodeOrNull<StaticBody2D>(previousAscent.Name+"Grip");
            var previousGripSurface=previousGripPost?.GetNodeOrNull<GrappleSurface2D>("GrappleSurfaceProfile");
            bool previousPostBlocker=previousGripSurface!=null && resolution.Surface==previousGripSurface;
            bool previousBodyPostBlocker=false;
            if(keyboardDriver!=null && mirroredAscent && ascentIndex>=4 && previousGripPost!=null &&
                castPost!=null && resolution.Surface==castPost.GetNode<GrappleSurface2D>("GrappleSurfaceProfile") &&
                player.IsOnFloor() && Task4HasFloorContact(player,previousAscent))
            {
                // Combat can leave the capsule just outside the old post's X
                // projection, yet the full diagonal pull still crosses it.
                // TestMove queries the real body without moving it; only this
                // exact original post may trigger the existing stance walk.
                var bodyCollision=player.GetNode<CollisionShape2D>("BodyCollider");
                TestAssert.True(!bodyCollision.Disabled && bodyCollision.Shape is CapsuleShape2D &&
                    resolution.Snapshot.Exists && !resolution.Snapshot.IsLegacyInteraction &&
                    resolution.Snapshot.RuntimeId==resolution.Surface.GetInstanceId() &&
                    resolution.Snapshot.WorldAnchor.IsFinite() && resolution.Snapshot.SurfaceNormal.IsFinite() &&
                    !resolution.Snapshot.SurfaceNormal.IsZeroApprox(),
                    "body-path preparation uses the enabled capsule and finite real intended-post snapshot");
                var capsule=(CapsuleShape2D)bodyCollision.Shape;
                var settings=MechanicalClawSettings.Default;
                Vector2 dock=MechanicalClawArrivalGeometry.DockPoint(resolution.Snapshot.WorldAnchor,
                    resolution.Snapshot.SurfaceNormal,capsule,settings.MinimumRopeLength,settings.ArrivalMargin);
                TestAssert.True(dock.IsFinite(),"body-path preparation has a finite predicted dock before the native query");
                using var predictedContact=new KinematicCollision2D();
                previousBodyPostBlocker=player.TestMove(player.GlobalTransform,dock-player.GlobalPosition,
                    predictedContact,player.SafeMargin,false) && predictedContact.GetCollider()==previousGripPost;
                if(previousBodyPostBlocker)
                    GD.Print($"TASK4_ASCENT_BODY_PREFLIGHT support={support.Name} player={player.GlobalPosition} " +
                        $"dock={dock} blocker={previousGripPost.GetPath()} travel={predictedContact.GetTravel()} " +
                        $"normal={predictedContact.GetNormal()} body_moved=false");
            }
            if(keyboardDriver!=null && gripPost!=null && mirroredAscent &&
                (resolution.Surface==support.GetNode<GrappleSurface2D>("GrappleSurfaceProfile") ||
                    previousPostBlocker || previousBodyPostBlocker))
            {
                var previous=previousAscent;
                var blocker=resolution.Surface;
                TestAssert.True(previous!=null && previous.GetParent()==support.GetParent() &&
                    (!previousPostBlocker || previousGripPost.GetParent()==previous.GetParent() &&
                        previousGripSurface.GetParent()==previousGripPost && previousGripSurface.UseCollisionHitPoint &&
                        previousGripSurface.Material==GrappleSurfaceMaterial.TempleMetal &&
                        previousGripSurface is not ThreadTarget2D && resolution.Snapshot.Exists &&
                        !resolution.Snapshot.IsLegacyInteraction &&
                        resolution.Snapshot.RuntimeId==previousGripSurface.GetInstanceId()),
                    $"preparation blocker is only the target platform or the exact previous support's authored metal post: " +
                    $"support={support.Name} previous={previous?.GetPath()} blocker={blocker?.GetPath()} " +
                    $"material={blocker?.Material} previous_post={previousPostBlocker} " +
                    $"snapshot={resolution.Snapshot.Exists}/{resolution.Snapshot.RuntimeId}/{resolution.Snapshot.IsLegacyInteraction}");
                var previousCollision=previous.GetNode<CollisionShape2D>("Collision");
                var targetCollision=support.GetNode<CollisionShape2D>("Collision");
                var bodyCollision=player.GetNode<CollisionShape2D>("BodyCollider");
                var health=player.GetNode<HealthComponent>("Health");
                var motor=player.GetNode<PlayerMotor>("Motor");
                TestAssert.True(previousCollision.Shape is RectangleShape2D &&
                    bodyCollision.Shape is CapsuleShape2D && !previousCollision.Disabled && !bodyCollision.Disabled,
                    "keyboard mirrored ascent prepares on enabled real rectangle/capsule geometry");
                var previousShape=(RectangleShape2D)previousCollision.Shape;
                var capsule=(CapsuleShape2D)bodyCollision.Shape;
                Transform2D bodyTransform=bodyCollision.GlobalTransform;
                TestAssert.True(bodyTransform.Origin.IsFinite() && bodyTransform.X.IsFinite() && bodyTransform.Y.IsFinite() &&
                    Mathf.Abs(bodyTransform.X.Y)<.001f && Mathf.Abs(bodyTransform.Y.X)<.001f &&
                    Mathf.Abs(previousCollision.GlobalTransform.X.Y)<.001f && Mathf.Abs(previousCollision.GlobalTransform.Y.X)<.001f &&
                    Mathf.Abs(targetCollision.GlobalTransform.X.Y)<.001f && Mathf.Abs(targetCollision.GlobalTransform.Y.X)<.001f,
                    "keyboard mirrored ascent uses finite upright actual support/body transforms");
                Vector2 bodyHalf=new(capsule.Radius*Mathf.Abs(bodyTransform.X.X),
                    capsule.Height*.5f*Mathf.Abs(bodyTransform.Y.Y));
                TestAssert.True(bodyHalf.IsFinite() && bodyHalf.X>0 && bodyHalf.Y>=bodyHalf.X &&
                    Mathf.Abs(plannedDirection.X)>.7f && plannedDirection.Y<-.7f && plannedDirection.X*direction>0,
                    "keyboard mirrored ascent requires an actual diagonal up octant and full capsule extents");
                float previousLeft=previousCollision.ToGlobal(new Vector2(-previousShape.Size.X/2,0)).X;
                float previousRight=previousCollision.ToGlobal(new Vector2(previousShape.Size.X/2,0)).X;
                float previousTop=previousCollision.ToGlobal(new Vector2(0,-previousShape.Size.Y/2)).Y;
                float targetNear=targetCollision.ToGlobal(new Vector2(-direction*shape.Size.X/2,0)).X;
                float targetTop=targetCollision.ToGlobal(new Vector2(0,-shape.Size.Y/2)).Y;
                const float cornerMargin=4f;
                float slope=Mathf.Abs(plannedDirection.Y/plannedDirection.X);
                Vector2 from=player.GlobalPosition;
                float originLimit=targetNear-direction*(bodyHalf.X+cornerMargin+
                    (from.Y-targetTop+bodyHalf.Y+cornerMargin)/slope);
                // Four extra pixels cover discrete braking and the genuine K
                // admission's one sampled A/W or D/W motor step.
                float stanceX=originLimit-direction*4f;
                TestAssert.True(float.IsFinite(stanceX) &&
                    Mathf.Abs(from.X-stanceX)<=100f && stanceX-bodyHalf.X>=previousLeft+cornerMargin &&
                    stanceX+bodyHalf.X<=previousRight-cornerMargin &&
                    float.IsFinite(motor.Tuning.GroundDeceleration) && motor.Tuning.GroundDeceleration>0,
                    "one computed mirrored-ascent stance fits the whole original support within100px");
                PrologueLevelController routeLevel=null;
                for(Node ancestor=player.GetParent();ancestor!=null;ancestor=ancestor.GetParent())
                    if(ancestor is PrologueLevelController owner) { routeLevel=owner; break; }
                TestAssert.True(routeLevel!=null && routeLevel.Player==player,
                    "keyboard mirrored ascent retains the original route player/recovery owner");
                int hp=health.CurrentHealth, jumps=motor.AcceptedJumpCount, dashes=motor.AcceptedDashCount;
                int rawClaws=keyboardDriver.ClawEdges, rawAttacks=keyboardDriver.AttackEdges;
                int recoveryRequests=0, recoveries=0, preparationFrames=0, settleFrames=0;
                ulong nextCast=claw.CurrentFrame.NextCastId, preparationSequence=input.CurrentFrameSequence;
                ulong preparationPhysics=Engine.GetPhysicsFrames();
                void Requested(string _) => recoveryRequests++;
                void Recovered(string _) => recoveries++;
                void PrepareGuard()
                {
                    TestAssert.True(GodotObject.IsInstanceValid(player) && GodotObject.IsInstanceValid(previous) &&
                        GodotObject.IsInstanceValid(routeLevel) && routeLevel.Player==player &&
                        player.IsInsideTree() && previous.IsInsideTree() && !tree.Paused &&
                        health.CurrentHealth==hp && recoveryRequests==0 && recoveries==0 &&
                        motor.AcceptedJumpCount==jumps && motor.AcceptedDashCount==dashes &&
                        keyboardDriver.ClawEdges==rawClaws && keyboardDriver.AttackEdges==rawAttacks &&
                        claw.CurrentFrame.State==MechanicalClawState.Idle && claw.CurrentFrame.NextCastId==nextCast &&
                        claw.CurrentFrame.Failure==MechanicalClawFailure.None && claw.CurrentTarget==null &&
                        player.GlobalPosition.IsFinite() && player.Velocity.IsFinite(),
                        "mirrored-ascent preparation preserves original HP/recovery/jump/dash/cast baselines");
                    TestAssert.True(input.CurrentFrame.KeyboardActions &&
                        !input.CurrentFrame.PrimaryPressed && !input.CurrentFrame.PrimaryHeld && !input.CurrentFrame.PrimaryReleased &&
                        !input.CurrentFrame.GrappleModePressed && !input.CurrentFrame.AttackModePressed &&
                        !input.CurrentFrame.ClawPressed && !input.CurrentFrame.AttackPressed && !input.CurrentFrame.InteractPressed &&
                        !input.CurrentFrame.JumpPressed && !input.CurrentFrame.JumpHeld && !input.CurrentFrame.DashPressed &&
                        !input.CurrentFrame.ShieldHeld && !Input.IsActionPressed(InputActions.Claw) &&
                        !Input.IsActionPressed(InputActions.Attack) && !Input.IsActionPressed(InputActions.Interact) &&
                        !Input.IsActionPressed(InputActions.Jump) && !Input.IsActionPressed(InputActions.Dash) &&
                        !Input.IsActionPressed(InputActions.MoveUp) && !Input.IsActionPressed(InputActions.MoveDown),
                        "mirrored-ascent preparation uses only real A/D, without a cast, jump, shield or legacy input");
                    Vector2 bodyCenter=bodyCollision.GlobalPosition;
                    Rect2 bodyBounds=new(bodyCenter-bodyHalf,bodyHalf*2);
                    TestAssert.True(player.IsOnFloor() && !previousCollision.Disabled &&
                        bodyBounds.Position.X>=previousLeft && bodyBounds.End.X<=previousRight &&
                        Mathf.Abs(bodyBounds.End.Y-previousTop)<=1 && Mathf.Abs(player.Velocity.Y)<=.01f,
                        "mirrored-ascent preparation keeps the full capsule on its original previous platform");
                    foreach(float footX in new[]{bodyCenter.X,bodyBounds.Position.X+1,bodyBounds.End.X-1})
                    {
                        using var query=PhysicsRayQueryParameters2D.Create(new Vector2(footX,bodyBounds.End.Y-.25f),
                            new Vector2(footX,bodyBounds.End.Y+1),CombatCollisionLayers.World);
                        query.CollideWithBodies=true; query.CollideWithAreas=false;
                        using var hit=player.GetWorld2D().DirectSpaceState.IntersectRay(query);
                        TestAssert.True(hit.Count>0 && hit.ContainsKey("collider") && hit.ContainsKey("normal") &&
                            hit.ContainsKey("position") && hit["collider"].AsGodotObject()==previous &&
                            hit["normal"].AsVector2().Y<-.99f &&
                            Mathf.Abs(hit["position"].AsVector2().Y-bodyBounds.End.Y)<=1,
                            "mirrored-ascent center/edge feet are physically supported by the same original platform");
                    }
                }
                async Task PreparationTick()
                { PrepareGuard(); await PhysicsFrames(tree,1); PrepareGuard(); }
                routeLevel.RecoveryRequested+=Requested; routeLevel.RecoveryCompleted+=Recovered;
                try
                {
                    PrepareGuard();
                    // The previous helper's three signals do not imply zero
                    // velocity. Neutral sampling/deceleration consumes this same
                    // 40-frame preparation budget before the one-direction walk.
                    for(int frame=0;frame<12 && (Mathf.Abs(player.Velocity.X)>2 ||
                        input.CurrentFrameSequence<=preparationSequence || input.CurrentFrame.MoveAxis!=0);frame++)
                    { await PreparationTick(); preparationFrames++; }
                    TestAssert.True(Mathf.Abs(player.Velocity.X)<=2 && input.CurrentFrameSequence>preparationSequence &&
                        input.CurrentFrame.MoveAxis==0,"mirrored-ascent walking begins from fresh physically neutral support");
                    // Combat may leave the player on either side of this stance.
                    // Walking direction is independent of the intended K octant.
                    int walkDirection=Math.Sign(stanceX-player.GlobalPosition.X);
                    float projectedStopX=float.NaN;
                    int stopForecastSteps=0;
                    SendAction(InputActions.MoveLeft,walkDirection<0); SendAction(InputActions.MoveRight,walkDirection>0);
                    while(preparationFrames<40)
                    {
                        var stop=FreeMotionForecast.Neutral(player.Velocity.X,true,motor.MotionSettings,
                            motor.ThrusterState.Module,1d/Engine.PhysicsTicksPerSecond,12,2f);
                        TestAssert.True(stop.SpeedThresholdReached,
                            "mirrored-ascent neutral forecast fits original12-frame settling window");
                        projectedStopX=player.GlobalPosition.X+stop.NetDistance;
                        stopForecastSteps=stop.Steps;
                        float braking=Mathf.Max(0,walkDirection*stop.NetDistance);
                        if(walkDirection==0 || (stanceX-player.GlobalPosition.X)*walkDirection<=braking) break;
                        await PreparationTick(); preparationFrames++;
                    }
                    SendAction(InputActions.MoveLeft,false); SendAction(InputActions.MoveRight,false);
                    ulong stopSequence=input.CurrentFrameSequence;
                    while(settleFrames<12 && (Mathf.Abs(player.Velocity.X)>2 ||
                        input.CurrentFrameSequence<=stopSequence || input.CurrentFrame.MoveAxis!=0))
                    { await PreparationTick(); settleFrames++; }
                    PrepareGuard();
                    plannedDirection=PlayerKeyboardAimModel.ResolveDirection(aim-player.GlobalPosition,direction);
                    resolution=resolver.ResolveKeyboard(player,plannedDirection);
                    float cornerRise=(targetNear-direction*(bodyHalf.X+cornerMargin)-player.GlobalPosition.X)*direction*slope;
                    float clearance=targetTop-(player.GlobalPosition.Y-cornerRise+bodyHalf.Y);
                    GD.Print($"TASK4_KEYBOARD_STANCE support={support.Name} previous={previous.Name} from={from} to={player.GlobalPosition} " +
                        $"blocker={(previousBodyPostBlocker ? previousGripPost.GetPath() : blocker.GetPath())} " +
                        $"blocker_kind={(previousBodyPostBlocker ? "previous-body-post" : previousPostBlocker ? "previous-post" : "target-platform")} walk_direction={walkDirection} " +
                        $"stance_x={stanceX} limit_x={originLimit} body_half={bodyHalf} bbox_margin={cornerMargin} " +
                        $"corner={new Vector2(targetNear,targetTop)} clearance={clearance} budget={preparationFrames}/{settleFrames} " +
                        $"predicted_stop_x={projectedStopX:R} forecast_steps={stopForecastSteps}/12 " +
                        $"sample={preparationSequence}->{input.CurrentFrameSequence} hp={hp}->{health.CurrentHealth} " +
                        $"recovery={recoveryRequests}/{recoveries} selected={resolution.Surface?.GetPath()}");
                    TestAssert.True(preparationFrames<=40 && settleFrames<=12 &&
                        Engine.GetPhysicsFrames()-preparationPhysics<=52 && input.CurrentFrameSequence>stopSequence &&
                        input.CurrentFrame.MoveAxis==0 && Mathf.Abs(player.Velocity.X)<=2 &&
                        Mathf.Abs(player.GlobalPosition.X-stanceX)<=4f &&
                        direction*(player.GlobalPosition.X-originLimit)<=0 && clearance>=cornerMargin-.01f,
                        "one bounded real mirrored-ascent walk settles with full-capsule corner clearance before K");
                }
                finally
                {
                    SendAction(InputActions.MoveLeft,false); SendAction(InputActions.MoveRight,false);
                    if(GodotObject.IsInstanceValid(routeLevel))
                    { routeLevel.RecoveryRequested-=Requested; routeLevel.RecoveryCompleted-=Recovered; }
                }
            }
            GD.Print($"TASK4_CAST_RESOLUTION support={support.Name} profile={resolution.Surface?.GetPath()} first_hit={(GodotObject.InstanceFromId(resolution.Snapshot.RuntimeId) as Node)?.GetPath()} anchor={resolution.Snapshot.WorldAnchor} hit_distance={player.GlobalPosition.DistanceTo(resolution.Snapshot.WorldAnchor)} max_range={MechanicalClawSettings.Default.MaxRange}");
            if(castPost!=null) TestAssert.Equal(castPost.GetNode<GrappleSurface2D>("GrappleSurfaceProfile"),resolution.Surface,
                $"{support.Name} {(keyboardDriver!=null ? "planned keyboard octant" : "cursor")} resolves the real grip before input, not an intervening receiver");
            if(keyboardShortcutSideBoost) TestAssert.True(resolution.Surface==sideSurface &&
                Mathf.Abs(plannedDirection.Y)<.001f && plannedDirection.X*direction>.999f &&
                resolution.Snapshot.SurfaceNormal.Dot(-plannedDirection)>.999f &&
                resolution.Snapshot.WorldAnchor.Y>=top+4 && resolution.Snapshot.WorldAnchor.Y<=top+shape.Size.Y-4,
                "side/Boost preflight is the exact original platform side, not its post or a third-party surface");
            GD.Print($"TASK4_SUPPORT_CAST target={support.Name} player={player.GlobalPosition} aim={aim}");
            if (keyboardDriver != null && mirroredAscent && ascentIndex >= 4 &&
                previousAscent != null && previousGripPost != null && castPost != null)
            {
                // A clear aiming ray does not establish full-body clearance.
                // Only when the original post overlaps the capsule's upward
                // envelope, walk out from under it during this same cast's
                // travel. The accepted target and production collisions remain
                // authoritative; mirrored shelves use the same geometry rule.
                ascentSource = previousAscent;
                var oldPostCollision = previousGripPost.GetNode<CollisionShape2D>("Collision");
                var sourceCollision = ascentSource.GetNode<CollisionShape2D>("Collision");
                ascentBody = player.GetNode<CollisionShape2D>("BodyCollider");
                TestAssert.True(oldPostCollision.Shape is RectangleShape2D &&
                    sourceCollision.Shape is RectangleShape2D && ascentBody.Shape is CapsuleShape2D &&
                    !oldPostCollision.Disabled && !sourceCollision.Disabled && !ascentBody.Disabled,
                    "same-cast clearance uses enabled original post, shelf and capsule");
                foreach (var collision in new[] { oldPostCollision, sourceCollision, ascentBody })
                {
                    Transform2D transform = collision.GlobalTransform;
                    TestAssert.True(transform.Origin.IsFinite() && transform.X.IsFinite() && transform.Y.IsFinite() &&
                        Mathf.Abs(transform.X.Y) < .001f && Mathf.Abs(transform.Y.X) < .001f &&
                        Mathf.Abs(transform.X.X) > .001f && Mathf.Abs(transform.Y.Y) > .001f,
                        "same-cast clearance requires finite upright nondegenerate actual geometry");
                }
                var postShape = (RectangleShape2D)oldPostCollision.Shape;
                var sourceShape = (RectangleShape2D)sourceCollision.Shape;
                var capsule = (CapsuleShape2D)ascentBody.Shape;
                float radius = capsule.Radius * Mathf.Abs(ascentBody.GlobalTransform.X.X);
                float halfHeight = capsule.Height * .5f * Mathf.Abs(ascentBody.GlobalTransform.Y.Y);
                Vector2 bodyCenter = ascentBody.GlobalPosition;
                Vector2 postHalf = new(postShape.Size.X * .5f * Mathf.Abs(oldPostCollision.GlobalTransform.X.X),
                    postShape.Size.Y * .5f * Mathf.Abs(oldPostCollision.GlobalTransform.Y.Y));
                Rect2 postBounds = new(oldPostCollision.GlobalPosition - postHalf, postHalf * 2);
                float sourceHalfWidth = sourceShape.Size.X * .5f * Mathf.Abs(sourceCollision.GlobalTransform.X.X);
                float sourceLeft = sourceCollision.GlobalPosition.X - sourceHalfWidth;
                float sourceRight = sourceCollision.GlobalPosition.X + sourceHalfWidth;
                float outgoingEdge = direction > 0 ? postBounds.End.X : postBounds.Position.X;
                ascentClearX = outgoingEdge + direction * (radius + 4);
                float riseTop = Mathf.Min(bodyCenter.Y, resolution.Snapshot.WorldAnchor.Y) - halfHeight;
                float riseBottom = Mathf.Max(bodyCenter.Y, resolution.Snapshot.WorldAnchor.Y) + halfHeight;
                bool overlapsPostEnvelope = bodyCenter.X + radius > postBounds.Position.X &&
                    bodyCenter.X - radius < postBounds.End.X &&
                    riseTop < postBounds.End.Y && riseBottom > postBounds.Position.Y;
                ascentWalkPending = overlapsPostEnvelope && direction * (ascentClearX - bodyCenter.X) > 0;
                if (ascentWalkPending)
                    TestAssert.True(radius > 0 && halfHeight >= radius && player.IsOnFloor() &&
                        Task4HasFloorContact(player, ascentSource) &&
                        direction * (ascentClearX - bodyCenter.X) < 60 &&
                        Mathf.Min(bodyCenter.X, ascentClearX) - radius >= sourceLeft + 4 &&
                        Mathf.Max(bodyCenter.X, ascentClearX) + radius <= sourceRight - 4,
                        "same-cast clearance fits the full capsule and entire walk on the original shelf within60px");
            }
            if(keyboardDriver!=null)
            {
                ulong issueSequence=input.CurrentFrameSequence, sampledSequence=0, lastObservedSequence=issueSequence;
                ulong initialNextCast=claw.CurrentFrame.NextCastId;
                int initialKeyEdges=keyboardDriver.ClawEdges, samples=0;
                string Facts() => $"support={support.Name} physics={Engine.GetPhysicsFrames()} " +
                    $"issue={issueSequence} sample={input.CurrentFrameSequence} accepted={sampledSequence} samples={samples} " +
                    $"player={player.GlobalPosition} velocity={player.Velocity} planned={plannedDirection} " +
                    $"aim={input.CurrentFrame.Aim} actual={input.KeyboardAimDirection} K={input.CurrentFrame.ClawPressed} " +
                    $"heldK={Input.IsPhysicalKeyPressed(Key.K)} keyboard={input.CurrentFrame.KeyboardActions} " +
                    $"claw={claw.CurrentFrame.State}/{claw.CurrentFrame.CastId}/{claw.CurrentFrame.NextCastId} " +
                    $"target={claw.CurrentTarget?.GetPath()} preview={claw.KeyboardPreview.Surface?.GetPath()}";
                GD.Print($"TASK4_KEYBOARD_CAST_SAMPLE boundary=before-key {Facts()}");
                TestAssert.True(claw.CurrentFrame.State==MechanicalClawState.Idle,
                    $"keyboard support cast starts from natural Idle; {Facts()}");
                try
                {
                    SendMouse(MouseButton.Left,true,aim);
                    if(ascentGrip8Diagnostic) Task10TraceAscentGrip8(player,claw,"issued-press",-1);
                    // PhysicsFrame precedes InputReader's callbacks. Releasing K
                    // after one signal can remove its temporary WASD before the
                    // accepted edge is sampled, leaving only the old facing.
                    for(int frame=0;frame<8;frame++)
                    {
                        await PhysicsFrames(tree,1);
                        bool fresh=input.CurrentFrameSequence>lastObservedSequence;
                        if(fresh)
                        {
                            lastObservedSequence=input.CurrentFrameSequence;
                            if(input.CurrentFrame.ClawPressed)
                            { sampledSequence=input.CurrentFrameSequence; samples++; }
                        }
                        if(frame==0 || fresh)
                            GD.Print($"TASK4_KEYBOARD_CAST_SAMPLE boundary=held frame={frame} fresh={fresh} {Facts()}");
                        if(sampledSequence>issueSequence && claw.CurrentFrame.State is
                            MechanicalClawState.Aim or MechanicalClawState.Windup or MechanicalClawState.Travel) break;
                    }
                    TestAssert.True(sampledSequence>issueSequence && samples==1 &&
                        keyboardDriver.ClawEdges==initialKeyEdges+1 && Input.IsPhysicalKeyPressed(Key.K) &&
                        input.CurrentFrame.KeyboardActions &&
                        !input.CurrentFrame.PrimaryPressed && !input.CurrentFrame.PrimaryHeld && !input.CurrentFrame.PrimaryReleased &&
                        !input.CurrentFrame.AttackModePressed && !input.CurrentFrame.GrappleModePressed &&
                        !input.CurrentFrame.AttackPressed && !input.CurrentFrame.InteractPressed &&
                        !input.CurrentFrame.ShieldHeld && !input.CurrentFrame.LegacyShieldHeld &&
                        input.KeyboardAimDirection.Dot(plannedDirection)>.999f &&
                        claw.CurrentFrame.State is MechanicalClawState.Aim or MechanicalClawState.Windup or MechanicalClawState.Travel,
                        $"one fresh physical K retains its real octant through admission within8; {Facts()}");
                    var sampledResolution=resolver.ResolveKeyboard(player,input.KeyboardAimDirection);
                    TestAssert.True(sampledResolution.Surface==resolution.Surface && sampledResolution.Snapshot.HasLineOfSight &&
                        claw.CurrentTarget==resolution.Surface && claw.CurrentFrame.Failure==MechanicalClawFailure.None,
                        $"actual keyboard resolver and production cast select the preflight surface; " +
                        $"resolved={sampledResolution.Surface?.GetPath()} anchor={sampledResolution.Snapshot.WorldAnchor}; {Facts()}");
                    TestAssert.True(claw.CurrentFrame.State==MechanicalClawState.Aim
                        ? claw.CurrentFrame.CastId==0 && claw.CurrentFrame.NextCastId==initialNextCast
                        : claw.CurrentFrame.CastId==initialNextCast && claw.CurrentFrame.NextCastId==initialNextCast+1,
                        $"accepted K admits one new cast, not a cancel or retry; {Facts()}");
                    if(keyboardShortcutSideBoost) TestAssert.True(sideRaySafe(sampledResolution) &&
                        sampledResolution.Surface==sideSurface &&
                        sampledResolution.Snapshot.RuntimeId==sideSurface.GetInstanceId() &&
                        sampledResolution.Snapshot.SurfaceNormal.Dot(new Vector2(-direction,0))>.999f &&
                        sampledResolution.Snapshot.WorldAnchor.Y>=top+4 &&
                        sampledResolution.Snapshot.WorldAnchor.Y<=top+shape.Size.Y-4 &&
                        claw.CurrentFrame.AnchorPoint.Y>=top+4 && claw.CurrentFrame.AnchorPoint.Y<=top+shape.Size.Y-4 &&
                        sideInput.CurrentFrame.JumpHeld && !sideInput.CurrentFrame.JumpPressed,
                        "fresh actual horizontal K retains the exact side and four-pixel corner margin through admission");
                    GD.Print($"TASK4_KEYBOARD_CAST_SAMPLE boundary=admitted {Facts()}");
                    if(ascentGrip8Diagnostic) Task10TraceAscentGrip8(player,claw,"held-physics",0);
                }
                finally { SendMouse(MouseButton.Left,false,aim); }
                if (ascentWalkPending)
                {
                    ascentWalkStarted = true;
                    SendAction(ascentWalkKey, true);
                }
            }
            else
            {
                SendMouse(MouseButton.Left,true,aim);
                if(ascentGrip8Diagnostic) Task10TraceAscentGrip8(player,claw,"issued-press",-1);
                await PhysicsFrames(tree,1);
                if(ascentGrip8Diagnostic) Task10TraceAscentGrip8(player,claw,"held-physics",0);
                SendMouse(MouseButton.Left,false,aim);
            }
            if(ascentGrip8Diagnostic) Task10TraceAscentGrip8(player,claw,"issued-release",0);
        }
        else
        {
            SendAction(InputActions.MoveLeft,direction<0); SendAction(InputActions.MoveRight,direction>0);
            if(top <= player.GlobalPosition.Y+36+4) SendAction(InputActions.Jump,true);
            SteamTopSnapshot("ordinary-input-issued", "callsite-before-sampling");
        }
        if(shortcut2996Observation)
            Task23ObserveShortcut2996(player,support,claw,center,shape,clawRequired,
                direction,clawRequired ? "claw-input-issued" : "move-input-issued",-1,
                departed,ref shortcut2996Samples);
        for(int i=0;i<300;i++)
        {
            SideGuard();
            await PhysicsFrames(tree,1);
            SideGuard();
            if (ascentWalkPending)
            {
                TestAssert.True(sideHealth.CurrentHealth == sideHp && sideMotor.AcceptedJumpCount == sideJumps &&
                    sideMotor.AcceptedDashCount == sideDashes &&
                    claw.CurrentFrame.Failure == MechanicalClawFailure.None &&
                    claw.CurrentTarget == castPost.GetNode<GrappleSurface2D>("GrappleSurfaceProfile") &&
                    claw.CurrentFrame.CastId == sideCast &&
                    player.IsOnFloor() && Task4HasFloorContact(player, ascentSource),
                    "ascent clearance walk retains HP, original support and the single real admitted cast");
                TestAssert.True(claw.CurrentFrame.State is MechanicalClawState.Windup or MechanicalClawState.Travel or MechanicalClawState.Latched,
                    "ascent full capsule must clear the old post before automatic Reel begins");
                if (direction * (ascentBody.GlobalPosition.X - ascentClearX) >= 0)
                {
                    SendAction(ascentWalkKey, false);
                    ascentWalkPending = false;
                    GD.Print($"TASK4_ASCENT_TRAVEL_CLEAR support={support.Name} player={player.GlobalPosition} boundary={ascentClearX} " +
                        $"state={claw.CurrentFrame.State} cast={claw.CurrentFrame.CastId} hp={sideHealth.CurrentHealth} " +
                        $"jump={sideJumps}->{sideMotor.AcceptedJumpCount} physics={Engine.GetPhysicsFrames()}");
                }
            }
            if(keyboardShortcutSideBoost && sideBoostReceipt && i%10==0)
            {
                string sideContacts=string.Join(";",Enumerable.Range(0,player.GetSlideCollisionCount()).Select(index=>
                {
                    var contact=player.GetSlideCollision(index);
                    return $"{(contact.GetCollider() as Node)?.Name}:{contact.GetNormal()}";
                }));
                GD.Print($"TASK4_KEYBOARD_SIDE_POST_BOOST support={support.Name} physics={Engine.GetPhysicsFrames()-sideStart}/300 " +
                    $"position={player.GlobalPosition} velocity={player.Velocity} floor={player.IsOnFloor()} contacts={sideContacts} " +
                    $"hp={sideHp}->{sideHealth.CurrentHealth} jump={sideJumps}->{sideMotor.AcceptedJumpCount} next_cast={sideCast}->{claw.CurrentFrame.NextCastId} " +
                    $"input_jump={sideInput.CurrentFrame.JumpPressed}/{sideInput.CurrentFrame.JumpHeld} axis={sideInput.CurrentFrame.MoveAxis} " +
                    $"landing={sideMotor.LandingCount} fall={sideMotor.LastFallHeight}/{sideMotor.LastFallDamage}/{sideMotor.LastFallDamageApplied}");
            }
            if(keyboardShortcutSideBoost && sideBoostReceipt && sideBoostRiseSamples<2 &&
                Engine.GetPhysicsFrames()>sideBoostRisePhysics)
            {
                TestAssert.True(Engine.GetPhysicsFrames()==sideBoostRisePhysics+1UL &&
                    player.Velocity.Y<0 && player.GlobalPosition.Y<sideBoostY &&
                    sideMotor.AcceptedJumpCount==sideJumps+1,
                    "matching consumed Boost produces two consecutive real post-Motor upward samples, without another ordinary jump");
                sideBoostRisePhysics=Engine.GetPhysicsFrames(); sideBoostY=player.GlobalPosition.Y;
                sideBoostRiseSamples++;
            }
            if(shortcut2996Observation) shortcut2996LastLocalFrame=i;
            if(shortcut2996Observation && (i<6 || (i+1)%30==0))
                Task23ObserveShortcut2996(player,support,claw,center,shape,clawRequired,
                    direction,"after-physics-signal",i,departed,ref shortcut2996Samples);
            if(ascentGrip8Diagnostic && i<=6)
                Task10TraceAscentGrip8(player,claw,"post-release-physics",i);
            if(verboseTrace && (support.Name=="LiftEntryFloor" || support.Name=="BridgeExitStep1") && i<30)
            {
                string contacts=string.Join(";",Enumerable.Range(0,player.GetSlideCollisionCount()).Select(j=>
                {
                    var contact=player.GetSlideCollision(j);
                    return $"{(contact.GetCollider() as Node)?.Name}:{contact.GetNormal()}";
                }));
                GD.Print($"TASK4_ENTRY_CAPSULE frame={i} position={player.GlobalPosition} velocity={player.Velocity} contacts={contacts}");
            }
            if(boostTurnPending && (!keyboardShortcutSideBoost || sideBoostReceipt))
            {
                SendAction(InputActions.MoveLeft,direction<0); SendAction(InputActions.MoveRight,direction>0);
                boostTurnPending=false;
            }
            departed |= !player.IsOnFloor();
            bool currentlyAttached=claw.CurrentFrame.State is MechanicalClawState.Reel or MechanicalClawState.Swing or MechanicalClawState.Latched;
            attached |= currentlyAttached;
            bool sideDockArrived=false;
            if(keyboardShortcutSideBoost && currentlyAttached && claw.CurrentTarget==sideSurface &&
                claw.CurrentFrame.CastId==sideCast && claw.CurrentFrame.LockedTargetRuntimeId==sideSurface.GetInstanceId() &&
                MechanicalClawArrivalGeometry.IsArrived(player.GlobalPosition,claw.CurrentFrame.DockPoint,
                    MechanicalClawSettings.Default.ArrivalTolerance))
            {
                sideDockArrived=true;
                for(int contactIndex=0;contactIndex<player.GetSlideCollisionCount();contactIndex++)
                {
                    using var contact=player.GetSlideCollision(contactIndex);
                    if(contact.GetCollider() is Node collider &&
                        MechanicalClawTargetResolver.ColliderBelongsToSurface(collider,sideSurface) &&
                        contact.GetNormal().Normalized().Dot(claw.CurrentFrame.SurfaceNormal.Normalized())<.8f)
                        sideDockArrived=false;
                }
                if(sideDockArrived && !sideNativeArrivalSeen)
                {
                    sideNativeArrivalSeen=true; sideArrived=true;
                    GD.Print($"TASK4_KEYBOARD_SIDE_NATIVE_ARRIVAL support={support.Name} physics={Engine.GetPhysicsFrames()-sideStart}/300 " +
                        $"cast={sideCast} sequence={sideInput.CurrentFrameSequence} position={player.GlobalPosition} velocity={player.Velocity} " +
                        $"dock={claw.CurrentFrame.DockPoint} distance={player.GlobalPosition.DistanceTo(claw.CurrentFrame.DockPoint)} " +
                        $"tolerance={MechanicalClawSettings.Default.ArrivalTolerance} state={claw.CurrentFrame.State} published_arrived={claw.CurrentFrame.Arrived}");
                }
            }
            if(keyboardShortcutSideBoost && currentlyAttached && !releaseIssued &&
                player.GlobalPosition.DistanceTo(claw.CurrentFrame.DockPoint)<=40)
                GD.Print($"TASK4_KEYBOARD_SIDE_DOCK_PHASE support={support.Name} physics={Engine.GetPhysicsFrames()-sideStart}/300 " +
                    $"sequence={sideInput.CurrentFrameSequence} state={claw.CurrentFrame.State} published_arrived={claw.CurrentFrame.Arrived} " +
                    $"position={player.GlobalPosition} velocity={player.Velocity} dock={claw.CurrentFrame.DockPoint} " +
                    $"distance={player.GlobalPosition.DistanceTo(claw.CurrentFrame.DockPoint)} axis={sideInput.CurrentFrame.MoveAxis}");
            if(keyboardShortcutSideBoost && currentlyAttached && !sideJumpReleased)
            {
                TestAssert.True(sideAttached && claw.CurrentTarget==sideSurface &&
                    claw.CurrentFrame.CastId==sideCast && claw.CurrentFrame.LockedTargetRuntimeId==sideSurface.GetInstanceId(),
                    "only the actual matching platform attachment releases the first held Space");
                sideReleaseSequence=sideInput.CurrentFrameSequence;
                SendAction(InputActions.Jump,false); sideJumpReleased=true;
            }
            if(verboseTrace && currentlyAttached && !releaseIssued) Task4DiagnosePull(player,claw,i);
            if(currentlyAttached && castPost != null && !releaseIssued)
                TestAssert.Equal(castPost.GetNode<GrappleSurface2D>("GrappleSurfaceProfile"),claw.CurrentTarget,
                    "shortcut pull hits its real above-shelf post, not an intervening platform corner");
            // The actual completed Motor pose can enter the production arrival
            // envelope before the next Claw publication. Do not add an extra
            // Swing/gravity tick by waiting for that already-stale label.
            if(clawRequired && !releaseIssued &&
                (keyboardShortcutSideBoost ? sideDockArrived : claw.CurrentFrame.Arrived))
            {
                if(castPost != null)
                {
                    SendMouse(MouseButton.Left,true,claw.CurrentFrame.AnchorPoint);
                    await PhysicsFrames(tree,1);
                    SendMouse(MouseButton.Left,false,claw.CurrentFrame.AnchorPoint);
                    releaseIssued=true; // one release command; no Space/ordinary jump on a post
                    GD.Print($"TASK4_SUPPORT_RETRACT_ABOVE target={support.Name} player={player.GlobalPosition}");
                }
                else
                {
                if(keyboardShortcutSideBoost)
                {
                    TestAssert.True(sideAttached && sideArrived && sideNativeArrivalSeen && sideDockArrived && sideJumpReleased &&
                        sideInput.CurrentFrameSequence>sideReleaseSequence && !sideInput.CurrentFrame.JumpHeld &&
                        !sideInput.CurrentFrame.JumpPressed && !Input.IsPhysicalKeyPressed(Key.Space) &&
                        claw.CurrentTarget==sideSurface && claw.CurrentFrame.CastId==sideCast &&
                        claw.CurrentFrame.LockedTargetRuntimeId==sideSurface.GetInstanceId() &&
                        sideMotor.AcceptedJumpCount==sideJumps+1,
                        "a second fresh Space is allowed only after exact real attachment/Arrived and sampled release, never as a second ordinary jump");
                    sideBoostIssued=true;
                }
                // At a side-face dock, steering away chooses the upward tangent of the
                // production Space boost. Steer onto the broad shelf on the next tick.
                SendAction(InputActions.MoveLeft,direction>0); SendAction(InputActions.MoveRight,direction<0);
                SendAction(InputActions.Jump,true); releaseIssued=true; boostApplied=true; boostTurnPending=true;
                GD.Print($"TASK4_SUPPORT_BOOST target={support.Name} player={player.GlobalPosition}");
                }
            }
            if(MathF.Abs(player.GlobalPosition.X-center.X)<8)
            {
                if (steamTopTrace && !steamTopReleaseObserved)
                { SteamTopSnapshot("first-near-center-release", "callsite-pre-node"); steamTopReleaseObserved = true; }
                SendAction(InputActions.MoveLeft,false); SendAction(InputActions.MoveRight,false);
            }
            if(departed && MathF.Abs(player.GlobalPosition.X-center.X)<=shape.Size.X/2-20 && Task4HasFloorContact(player,support))
            {
                if(keyboardShortcutSideBoost) TestAssert.True(sideBoostReceipt && sideBoostRiseSamples==2 && !sideInvalidReceipt &&
                    sideAttached && sideArrived && sideKSamples==1 &&
                    KeyboardRouteInputDriver.Current.ClawEdges==sideEdges+1 &&
                    sideMotor.AcceptedJumpCount==sideJumps+1 && claw.CurrentFrame.NextCastId==sideCast+1UL,
                    "original platform landing requires one actual side cast and matching consumed Boost, not above-post/manual Cancel or an extra jump");
                TestAssert.True(!clawRequired || attached,$"{support.Name} requires a real terrain attachment");
                GD.Print($"TASK4_SUPPORT_OK target={support.Name} player={player.GlobalPosition} claw={clawRequired} manual_release={releaseIssued && !boostApplied} boost_input={boostApplied}");
                SteamTopSnapshot("accepted-before-release", "callsite-pre-node");
                if(shortcut2996Observation)
                    Task23ObserveShortcut2996(player,support,claw,center,shape,clawRequired,
                        direction,"accepted-before-release",i,departed,ref shortcut2996Samples);
                ReleaseAll();
                if(keyboardShortcutSideBoost)
                {
                    FullSideTarget();
                    for(int tail=0;tail<3;tail++)
                    { SideGuard(); await PhysicsFrames(tree,1); SideGuard(); FullSideTarget(); }
                }
                else await PhysicsFrames(tree,3);
                if(keyboardShortcutSideBoost)
                {
                    SideGuard();
                    TestAssert.True(sideMotor.AcceptedJumpCount==sideJumps+1 &&
                        sideBoostReceipt && sideKSamples==1 && claw.CurrentFrame.NextCastId==sideCast+1UL,
                        "side/Boost tail retains complete original target support and the unchanged one-jump/one-cast/HP contract");
                    GD.Print($"TASK4_KEYBOARD_SIDE_BOOST_OK support={support.Name} physics={Engine.GetPhysicsFrames()-sideStart}/300 " +
                        $"cast={sideCast} native_platform={sideSurface.GetInstanceId()} K_samples={sideKSamples} " +
                        $"ordinary_jump={sideJumps}->{sideMotor.AcceptedJumpCount} boost_receipt={sideBoostReceipt} post_motor_rise={sideBoostRiseSamples} " +
                        $"manual_cancel=false above_post=false player={player.GlobalPosition} hp={sideHp}->{sideHealth.CurrentHealth}");
                }
                return;
            }
            if(verboseTrace && i%60==0) GD.Print($"TASK4_SUPPORT_TRACE target={support.Name} frame={i} player={player.GlobalPosition} claw={claw.CurrentFrame.State} failure={claw.CurrentFrame.Failure}");
            if(verboseTrace && clawRequired && i<100 && i%5==0) GD.Print($"TASK4_SUPPORT_DETAIL frame={i} player={player.GlobalPosition} velocity={player.Velocity} state={claw.CurrentFrame.State} failure={claw.CurrentFrame.Failure} target={claw.CurrentTarget?.GetParent()?.Name} anchor={claw.CurrentFrame.AnchorPoint} dock={claw.CurrentFrame.DockPoint}");
            if(player.GlobalPosition.Y>4320) break;
        }
        if(shortcut2996Observation)
            Task23ObserveShortcut2996(player,support,claw,center,shape,clawRequired,
                direction,"failed-before-original-throw",shortcut2996LastLocalFrame,
                departed,ref shortcut2996Samples);
        SteamTopSnapshot("failed-before-original-throw", "callsite-pre-node");
        throw new InvalidOperationException($"Actual input/capsule failed support {support.Name}: position={player.GlobalPosition} state={claw.CurrentFrame.State} failure={claw.CurrentFrame.Failure}");
        }
        finally
        {
            if (steamTopTrace)
            {
                if (steamTopObserver != null)
                {
                    steamTopObserver.Sample = null;
                    if (GodotObject.IsInstanceValid(steamTopObserver)) steamTopObserver.QueueFree();
                }
                SteamTopSnapshot("finally-before-cleanup", "callsite-pre-node");
                foreach (string row in steamTopRows) GD.Print(row);
                GD.Print($"TASK4_STEAM_TOP_TRACE_END rows={steamTopRows.Count}/64 samples={steamTopSamples} " +
                    $"omitted={steamTopOmitted} faults={steamTopFaults} original300=true observer_only=true");
            }
            if (ascentWalkStarted) SendAction(ascentWalkKey, false);
            if (grip4Trace)
            {
                if (GodotObject.IsInstanceValid(claw)) claw.ClawFrameChanged -= ObserveGrip4;
                foreach (string row in grip4Rows) GD.Print(row);
                GD.Print($"TASK4_ASCENT_PASSIVE_END support={support.Name} rows={grip4Rows.Count}/64 omitted={grip4Omitted} observation_errors={grip4ObservationErrors} diagnostic_only=true");
            }
            if(keyboardShortcutSideBoost)
            {
                if(GodotObject.IsInstanceValid(claw)) claw.ClawFrameChanged-=ObserveSideFrame;
                ReleaseAll();
            }
        }
    }

    private static void Task23ObserveShortcut2996(CharacterBody2D player, StaticBody2D support,
        MechanicalClawController claw, Vector2 center, RectangleShape2D supportShape, bool clawRequired,
        int direction, string edge, int localFrame, bool departed, ref int samples)
    {
        // Read-only snapshots at existing call sites: PhysicsFrame is emitted before node
        // _PhysicsProcess callbacks. The input owner's sequence is not an engine-frame ID.
        // No extra wait, input reassertion, signal subscription or movement query is added.
        const int sampleCap = 32;
        const int contactCap = 8;
        if(samples>=sampleCap) return;
        int sample=++samples;
        var fields=new List<string>();
        int observerFaults=0;
        string Bounded(string value, int limit=320)
        {
            value=value.Replace('\r',' ').Replace('\n',' ');
            return value.Length<=limit ? value : value[..limit]+"...";
        }
        void Read(string name, Func<object> read)
        {
            try { fields.Add(name+"="+Bounded(read()?.ToString() ?? "null")); }
            catch(Exception exception)
            {
                observerFaults++;
                fields.Add(name+"=unavailable observer_exception="+name+":"+
                    exception.GetType().Name+":"+Bounded(exception.Message,160));
            }
        }
        try
        {
            Read("physics_frame",()=>Engine.GetPhysicsFrames());
            Read("monotonic_usec",()=>Time.GetTicksUsec());
            Read("player_path",()=>player.GetPath());
            Read("support_path",()=>support.GetPath());
            Read("support_size",()=>supportShape.Size);
            Read("accepted_x_left",()=>center.X-supportShape.Size.X/2+20);
            Read("accepted_x_right",()=>center.X+supportShape.Size.X/2-20);
            Read("position",()=>player.GlobalPosition);
            Read("velocity",()=>player.Velocity);
            Read("on_floor",()=>player.IsOnFloor());
            Read("floor_normal",()=>player.IsOnFloor() ? (object)player.GetFloorNormal() : "not-on-floor");
            Read("support_floor_contact",()=>Task4HasFloorContact(player,support));
            Read("full_horizontal_inset",()=>MathF.Abs(player.GlobalPosition.X-center.X)<=supportShape.Size.X/2-20);
            Read("acceptance_before_site",()=>departed &&
                MathF.Abs(player.GlobalPosition.X-center.X)<=supportShape.Size.X/2-20 && Task4HasFloorContact(player,support));
            Read("capsule_radius",()=>((CapsuleShape2D)player.GetNode<CollisionShape2D>("BodyCollider").Shape).Radius);
            Read("capsule_height",()=>((CapsuleShape2D)player.GetNode<CollisionShape2D>("BodyCollider").Shape).Height);
            Read("sample_sequence",()=>player.GetNode<PlayerInputReader>("Input").CurrentFrameSequence);
            Read("sampled_move_axis",()=>player.GetNode<PlayerInputReader>("Input").CurrentFrame.MoveAxis);
            Read("sampled_jump_held",()=>player.GetNode<PlayerInputReader>("Input").CurrentFrame.JumpHeld);
            Read("sampled_shield_held",()=>player.GetNode<PlayerInputReader>("Input").CurrentFrame.ShieldHeld);
            Read("sampled_primary_held",()=>player.GetNode<PlayerInputReader>("Input").CurrentFrame.PrimaryHeld);
            Read("input_move_axis",()=>Input.GetAxis(InputActions.MoveLeft,InputActions.MoveRight));
            foreach(var action in new[]{InputActions.MoveLeft,InputActions.MoveRight,
                InputActions.Jump,InputActions.Dash,InputActions.Shield,InputActions.PrimaryAction})
            {
                Read("held_"+action,()=>Input.IsActionPressed(action));
                Read("strength_"+action,()=>Input.GetActionStrength(action));
            }
            Read("ability_scale",()=>player.GetNode<PlayerMotor>("Motor").AbilityMovementScale);
            Read("shield_scale",()=>player.GetNode<PlayerMotor>("Motor").ShieldMovementScale);
            Read("grapple_scale",()=>player.GetNode<PlayerMotor>("Motor").GrappleMovementScale);
            Read("effective_scale",()=>player.GetNode<PlayerMotor>("Motor").EffectiveMovementScale);
            Read("motor_dashing",()=>player.GetNode<PlayerMotor>("Motor").IsDashing);
            Read("motor_release_momentum",()=>player.GetNode<PlayerMotor>("Motor").HasGrappleReleaseMomentum);
            Read("shield_active",()=>player.GetNode<PlayerShieldController>("Shield").IsActive);
            Read("shield_direction",()=>player.GetNode<PlayerShieldController>("Shield").GuardDirection);
            Read("window_focus",()=>player.GetWindow().HasFocus());
            Read("tree_paused",()=>player.GetTree().Paused);
            Read("claw_state",()=>claw.CurrentFrame.State);
            Read("claw_failure",()=>claw.CurrentFrame.Failure);
            Read("claw_cast",()=>claw.CurrentFrame.CastId);
            Read("claw_locked_target",()=>claw.CurrentFrame.LockedTargetRuntimeId);
            Read("claw_target_path",()=>claw.CurrentTarget?.GetPath());
            Read("claw_correction",()=>claw.CurrentFrame.VelocityCorrection);
            int contactCount=0;
            Read("contact_count",()=>contactCount=player.GetSlideCollisionCount());
            for(int contactIndex=0;contactIndex<Math.Min(contactCount,contactCap);contactIndex++)
            {
                int index=contactIndex;
                Read("contact_"+index+"_path",()=>
                    (player.GetSlideCollision(index).GetCollider() as Node)?.GetPath());
                Read("contact_"+index+"_normal",()=>player.GetSlideCollision(index).GetNormal());
            }
            fields.Add("contacts_omitted="+Math.Max(0,contactCount-contactCap));
            GD.Print($"TASK23_SHORTCUT2996_OBSERVATION sample={sample}/{sampleCap} edge={edge} " +
                $"local_frame={localFrame} timing={(edge=="after-physics-signal" ? "signal-resume-not-node-completion" : "sync-snapshot")} " +
                $"claw_required={clawRequired} direction={direction} departed_before_site={departed} " +
                $"support_center={center} " +
                string.Join(" ",fields)+$" observer_faults={observerFaults}");
        }
        catch(Exception exception)
        {
            // A diagnostic fault is separate from, and must never replace, the original test.
            try { GD.Print($"TASK23_SHORTCUT2996_OBSERVER_EXCEPTION sample={sample} edge={edge} " +
                $"type={exception.GetType().Name} message={Bounded(exception.Message,160)}"); }
            catch(Exception) { /* If the output sink also fails, preserve traversal control flow. */ }
        }
    }

    private static void Task10TraceAscentGrip8(CharacterBody2D player,
        MechanicalClawController claw, string edge, int localFrame)
    {
        string contacts=string.Join(";",Enumerable.Range(0,player.GetSlideCollisionCount()).Select(index=>
        {
            KinematicCollision2D contact=player.GetSlideCollision(index);
            return $"{(contact.GetCollider() as Node)?.GetPath()}:{contact.GetNormal()}";
        }));
        PlayerInputReader input=player.GetNodeOrNull<PlayerInputReader>("Input");
        PrologueLevelController level=null;
        for(Node current=player;current!=null;current=current.GetParent())
            if(current is PrologueLevelController match){level=match;break;}
        PrologueGameFlowController flow=level?.GetNodeOrNull<PrologueGameFlowController>("GameFlow");
        HealthComponent health=player.GetNodeOrNull<HealthComponent>("Health");
        MechanicalClawFrame frame=claw.CurrentFrame;
        GD.Print($"TASK10_ASCENT_GRIP8_DIAGNOSTIC edge={edge} local_frame={localFrame} " +
            $"physics={Engine.GetPhysicsFrames()} issued_left={edge is "issued-press"} " +
            $"actual_left={Input.IsMouseButtonPressed(MouseButton.Left)} " +
            $"input_sequence={input?.CurrentFrameSequence} primary_pressed={input?.CurrentFrame.PrimaryPressed} " +
            $"primary_released={input?.CurrentFrame.PrimaryReleased} state={frame.State} " +
            $"failure={frame.Failure} target_instance={claw.CurrentTarget?.GetInstanceId() ?? 0} " +
            $"locked_target={frame.LockedTargetRuntimeId} anchor={frame.AnchorPoint} dock={frame.DockPoint} " +
            $"player={player.GlobalPosition} velocity={player.Velocity} contacts={contacts} " +
            $"flow={flow?.State} health={health?.CurrentHealth}/{health?.MaxHealth} " +
            $"paused={player.GetTree()?.Paused}");
    }

    private static void Task4DiagnosePull(CharacterBody2D player, MechanicalClawController claw, int frame)
    {
        var previous=claw.CurrentFrame with { CasterWorldPosition=player.GlobalPosition,CasterVelocity=player.Velocity };
        var snapshot=new MechanicalClawTargetResolver().SnapshotLocked(player,claw.CurrentTarget,previous.AnchorPoint,previous.SurfaceNormal);
        bool arrived=MechanicalClawArrivalGeometry.IsArrived(player.GlobalPosition,previous.DockPoint,MechanicalClawSettings.Default.ArrivalTolerance);
        var next=new MechanicalClawModel().Step(previous,new MechanicalClawInput(MechanicalClawCommand.None,0,arrived,previous.DockPoint),snapshot,1f/Engine.PhysicsTicksPerSecond);
        Vector2 motion=(player.Velocity+next.VelocityCorrection-previous.VelocityCorrection)/Engine.PhysicsTicksPerSecond;
        using var collision=new KinematicCollision2D();
        bool blocked=player.TestMove(player.GlobalTransform,motion,collision,player.SafeMargin,false);
        if(blocked || !snapshot.HasLineOfSight)
        {
            string blocker="none";
            if(MechanicalClawTargetResolver.TryGetLineOcclusion(player,claw.CurrentTarget,player.GlobalPosition,previous.AnchorPoint,out var point,out var normal))
                blocker=$"occluded at {point} normal={normal}";
            GD.Print($"TASK4_PULL_BOUNDARY frame={frame} player={player.GlobalPosition} visible={snapshot.HasLineOfSight} ray={blocker} blocked={blocked} collider={(collision.GetCollider() as Node)?.GetPath()} normal={collision.GetNormal()} travel={collision.GetTravel()} motion={motion} next_correction={next.VelocityCorrection}");
        }
    }

    private static async Task AbyssBodyProbe(SceneTree tree, int exitIndex)
    {
        ReleaseAll();
        var root = new Node2D { Name = "Task4AbyssBody" };
        var sector = GD.Load<PackedScene>("res://scenes/prologue/sectors/CanyonDescent.tscn").Instantiate<PrologueSector>();
        sector.Position = new Vector2(1280, 0);
        var player = NewPlayer();
        player.Position = new Vector2(2160, 364); // sole placement: at rest on the real commitment ledge
        player.GetNode<Camera2D>("Camera2D").Enabled = true;
        root.AddChild(sector);
        root.AddChild(player);
        tree.Root.AddChild(root);
        ActiveInputCanvas = player;
        try
        {
            TestAssert.True(sector.GetNodeOrNull<StaticBody2D>("Gameplay/Collision/AbyssCommitment") != null,
                "AbyssStartsOnRealCommitment: generated scene has physical Y=400 ledge");
            var motor = player.GetNode<PlayerMotor>("Motor");
            var claw = player.GetNode<MechanicalClawController>("MechanicalClaw");
            TestAssert.Near(1850, motor.Tuning.Gravity, .001f, "passive probe retains actual gravity");
            TestAssert.Near(780, motor.Tuning.MaxFallSpeed, .001f, "passive probe retains terminal base cap");
            await WaitForFloor(tree, player);
            TestAssert.True(player.Velocity.IsZeroApprox(), "abyss reference begins at rest on real support");
            // The only approach input leaves the ledge. Once airborne and centered, the
            // passive case releases it; there is no teleport, velocity write, timer or brake.
            SendAction(InputActions.MoveRight, true);
            int airborne = -1;
            int landedAt = -1;
            bool cast = false;
            bool attached = false;
            bool measurableBrake = false;
            bool releasedOverExit = false;
            ulong airbornePhysics = 0;
            ulong landedPhysics = 0;
            float maxFall = 0;
            int ticks = Engine.PhysicsTicksPerSecond;
            float[] exitTops = [700,2220,3080];
            StaticBody2D landing = sector.GetNode<StaticBody2D>(exitIndex < 0 ?
                "Gameplay/Collision/AbyssSafeBottom" : $"Gameplay/Collision/AbyssExit{exitIndex + 1}");
            for (int frame = 0; frame < 660; frame++)
            {
                await PhysicsFrames(tree, 1);
                if (!player.IsOnFloor() && airborne < 0)
                {
                    airborne = frame;
                    airbornePhysics = Engine.GetPhysicsFrames();
                    SendAction(InputActions.MoveRight, false);
                    GD.Print($"TASK4_ABYSS_ONSET frame={frame} position={player.GlobalPosition} velocity={player.Velocity} horizontal_released=True");
                }
                maxFall = MathF.Max(maxFall, player.Velocity.Y);
                if (exitIndex >= 0 && airborne >= 0 && !cast && player.GlobalPosition.Y >= exitTops[exitIndex] - (exitIndex == 0 ? 360 : 600))
                {
                    SendAction(InputActions.SelectGrappleMode, true);
                    await PhysicsFrames(tree, 1);
                    SendAction(InputActions.SelectGrappleMode, false);
                    Vector2 aim = new(exitIndex == 1 ? 1940 : 2510, exitTops[exitIndex] - (exitIndex == 0 ? 150 : 250));
                    SendMouse(MouseButton.Left, true, aim);
                    await PhysicsFrames(tree, 1);
                    SendMouse(MouseButton.Left, false, aim);
                    cast = true;
                }
                attached |= claw.CurrentFrame.State is MechanicalClawState.Latched or MechanicalClawState.Reel or MechanicalClawState.Swing or MechanicalClawState.Boost;
                measurableBrake |= attached && claw.CurrentFrame.VelocityCorrection.Y < -10;
                if (exitIndex >= 0 && measurableBrake && !releasedOverExit &&
                    (exitIndex == 1 ? player.GlobalPosition.X <= 2080 : player.GlobalPosition.X >= 2390))
                {
                    // A second real click retracts after braking over the shelf; gravity
                    // then completes the landing. No position/velocity correction is injected.
                    SendMouse(MouseButton.Left, true, claw.CurrentFrame.AnchorPoint);
                    await PhysicsFrames(tree, 1);
                    SendMouse(MouseButton.Left, false, claw.CurrentFrame.AnchorPoint);
                    releasedOverExit = true;
                    GD.Print($"TASK4_ABYSS_RETRACT exit={exitIndex} position={player.GlobalPosition}");
                }
                if (exitIndex < 0) TestAssert.Equal(Vector2.Zero, claw.CurrentFrame.VelocityCorrection,
                    "passive no-input descent keeps grapple correction exactly zero");
                if (frame % 30 == 0) GD.Print($"TASK4_ABYSS_TRACE exit={exitIndex} frame={frame} position={player.GlobalPosition} velocity={player.Velocity} claw={claw.CurrentFrame.State} correction={claw.CurrentFrame.VelocityCorrection}");
                if (airborne >= 0 && Task4HasFloorContact(player, landing))
                { landedAt = frame; landedPhysics = Engine.GetPhysicsFrames(); break; }
                if (player.GlobalPosition.Y > 3630) break;
            }
            TestAssert.True(landedAt > airborne && airborne >= 0, $"real capsule lands on required abyss support exit={exitIndex}; position={player.GlobalPosition} claw={claw.CurrentFrame.State}");
            // Include physics ticks spent dispatching the active cast separately from the
            // passive measurement; passive has no awaits beyond one tick per iteration.
            double duration = (landedPhysics - airbornePhysics) / (double)ticks;
            TestAssert.Equal(0, motor.AcceptedJumpCount, "abyss probe never borrows ordinary jump or double jump");
            TestAssert.Equal(0, motor.AcceptedDashCount, "abyss probe never borrows air dash");
            if (exitIndex < 0)
            {
                TestAssert.True(duration >= 4d, $"PassiveAbyssAtLeastFourSeconds: measured {duration:0.000}s must not be padded");
                TestAssert.True(maxFall <= 780.1f, "passive real body uses the unchanged base cap");
                var model = new PlayerMotionModel();
                var state = new PlayerMotionState();
                float distance = 0;
                int steps = 0;
                while (distance < 3160 && steps < 600)
                {
                    distance += model.Step(state, default, false, 1d / ticks).Velocity.Y / ticks;
                    steps++;
                }
                TestAssert.True(steps / (double)ticks >= 4d && steps / (double)ticks < 6d,
                    "actual PlayerMotionModel confirms the 3160px reference supports four, not unconditional six seconds");
            }
            else TestAssert.True(attached && measurableBrake, $"exit {exitIndex + 1} is reached using actual wall grapple braking, not passive estimate");
            GD.Print($"TASK4_ABYSS_LANDED exit={exitIndex} seconds={duration:0.0000} position={player.GlobalPosition} max_fall={maxFall:0.00} brake={measurableBrake} no_teleport=True");
        }
        finally
        {
            ReleaseAll();
            root.QueueFree();
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            await PhysicsFrames(tree, 3);
        }
    }

    private static bool Task4HasFloorContact(CharacterBody2D player, StaticBody2D support)
    {
        if (!player.IsOnFloor()) return false;
        for (int i = 0; i < player.GetSlideCollisionCount(); i++)
        {
            KinematicCollision2D collision = player.GetSlideCollision(i);
            if (collision.GetCollider() == support && collision.GetNormal().Y < -.7f) return true;
        }
        return false;
    }

    private const float PlayerCollisionRadius = 20f;
    private const float PlayerCollisionHalfHeight = 36f;
    private const float GripStoneCatchSafetyMargin = 12f;
    private const float GripStoneCatchHeadroom = 48f;
    private const int StableLandingRequirement = 12;
    private const float LandingMarginRatio = .15f;
    private const float LandingApproachTolerance = 8f;
    private const int SingleJumpBudget = 105;
    private const int AssistedBudget = 240;
    private const string ViewportInputTransport = "viewport+inputmap os_warp=False";
    private static readonly SectorCase[] Sectors =
    [
        new("CanyonEntrance", "res://scenes/prologue/sectors/CanyonEntrance.tscn"),
        new("CanyonDescent", "res://scenes/prologue/sectors/CanyonDescent.tscn"),
        new("CanyonBasin", "res://scenes/prologue/sectors/CanyonBasin.tscn"),
        new("CanyonAscent", "res://scenes/prologue/sectors/CanyonAscent.tscn"),
        new("LostTemple", "res://scenes/prologue/sectors/LostTemple.tscn")
    ];

    private static readonly HashSet<string> CompletedLessonActions = new(StringComparer.Ordinal);
    // Independent old teaching contracts. Never substitute these for V2 Main.
    private static readonly SectorCase[] LegacyTeachingSectors =
    [
        new("CanyonEntrance", "res://scenes/prologue/sample/legacy/CanyonEntrance.tscn"),
        new("CanyonDescent", "res://scenes/prologue/sample/legacy/CanyonDescent.tscn"),
        new("CanyonBasin", "res://scenes/prologue/sample/legacy/CanyonBasin.tscn"),
        new("CanyonAscent", "res://scenes/prologue/sample/legacy/CanyonAscent.tscn"),
        new("LostTemple", "res://scenes/prologue/sample/legacy/LostTemple.tscn")
    ];
    private static SceneTree ActiveTree;
    private static Node2D ActiveInputCanvas;

    public static async Task Run(SceneTree tree)
    {
        ActiveTree = tree;
        CompletedLessonActions.Clear();
        ReleaseAll();
        TestAssert.True(InputMap.HasAction(InputActions.SelectAttackMode) &&
            InputMap.HasAction(InputActions.SelectGrappleMode) &&
            InputMap.HasAction(InputActions.PrimaryAction),
            "canyon physical probes drive the real Q/E and mouse-left InputMap actions");
        await AssertBasinAscentSeamCameraProtectsWholeCapsule(tree);
        await AssertShieldLaneBehavior(tree);
        await AssertBellChainGateBehavior(tree);
        await AssertGripStoneLatchAndLanding(tree);
        await AssertGripStoneRejectsPhysicalMarginalLanding(tree);
        await AssertDenseTerrainWallLatch(tree);
        await AssertCeilingLatchBoostAndReel(tree);
        await AssertFallingSelfRescue(tree);
        await AssertAscentAnchorSightlineMatrix(tree);
        await AssertEveryCatchLayerReturns(tree);
        int mandatory = 0;
        foreach (SectorCase sectorCase in LegacyTeachingSectors)
        {
            using var manifestOwner = new LegacyTeachingSectorFixture(sectorCase, "LegacyTraversalManifest");
            PrologueSector manifest = manifestOwner.Sector;
            TraversalSegment2D[] segments = manifest.GetNode("Gameplay/TraversalContracts")
                .GetChildren().OfType<TraversalSegment2D>().ToArray();
            foreach (TraversalSegment2D segment in segments)
            {
                mandatory++;
                if (segment.Kind == TraversalSegmentKind.Assisted)
                    await AssertOrdinaryJumpCannotReplaceClaw(tree, sectorCase, segment.SegmentId.ToString());
                foreach (float fraction in new[] { .2f, .5f, .8f })
                    await RunMandatorySample(tree, sectorCase, segment.SegmentId.ToString(), fraction);
                GD.Print($"TASK8_ROUTE_SEGMENT_OK sector={sectorCase.Name} segment={segment.SegmentId} " +
                    $"samples=20,50,80 dash=0 abilities={segment.RequiredAbilities}");
            }
        }
        TestAssert.Equal(11, mandatory, "physical probe executes all eleven mandatory segments");
        TestAssert.Equal("attack|ice|shield", string.Join("|", CompletedLessonActions.Order()),
            "representative route samples perform the real attack, Ice rejection and shield inputs");
        await RunOptionalAndBossOnly(tree);
        ReleaseAll();
        GD.Print("TASK3_CANYON_TRAVERSAL_OK segments=11 samples_each=3 stable_frames=12 " +
            "margin=15% wall=True ceiling_boost=True auto_pull=True self_rescue=True " +
            "catch_returns=4 second_air_jump=0 dash=0 optional_pass_skip=4 boss_trigger=physical legacy_teaching=true v2_main=false");
    }

    public static async Task RunOptionalAndBossOnly(SceneTree tree)
    {
        ActiveTree = tree;
        await AssertOptionalPassAndSkip(tree, LegacyTeachingSectors[2], "SupplyCave");
        await AssertOptionalPassAndSkip(tree, LegacyTeachingSectors[1], "ArchiveAlcove");
        await AssertOptionalPassAndSkip(tree, LegacyTeachingSectors[2], "SalvageCache");
        await AssertOptionalPassAndSkip(tree, LegacyTeachingSectors[3], "HighRuin");
        await AssertTempleArenaLocksOnlyAfterTrigger(tree);
    }

    public static async Task RunCatchOneProbe(SceneTree tree)
    {
        ActiveTree = tree;
        ReleaseAll();
        GD.Print($"TASK3_CATCH_ONE_PHASE phase={CatchDirectionPhase()}");
        await AssertAscentAnchorSightlineMatrix(tree);
        await AssertCatchLayerReturns(tree,
            new CatchReturnCase(LegacyTeachingSectors[3], "ascent_grapple_one", "CatchOne",
                "Gameplay/Collision/CatchReturnEdgeOne/GrappleSurfaceProfile",
                new Vector2(822, 1070), true));
        await AssertCatchLayerReturns(tree,
            new CatchReturnCase(LegacyTeachingSectors[3], "ascent_grapple_two", "CatchTwo",
                "Gameplay/Collision/CatchReturnWallTwo/GrappleSurfaceProfile",
                new Vector2(1040, 790), true));
        await AssertCatchLayerReturns(tree,
            new CatchReturnCase(LegacyTeachingSectors[3], "ascent_grapple_three", "CatchThree",
                "Gameplay/Collision/CatchReturnCeilingThree/GrappleSurfaceProfile",
                new Vector2(890, 618), true));
        ReleaseAll();
        GD.Print("TASK3_ASCENT_CATCH_PROBE_OK");
    }

    private static async Task RunMandatorySample(SceneTree tree, SectorCase sectorCase,
        string segmentId, float fraction, bool traceGateBoundary = false)
    {
        ReleaseAll();
        using var owned = new LegacyTeachingSectorFixture(sectorCase,
            $"Task8_{sectorCase.Name}_{segmentId}_{fraction:0.0}");
        var fixture = owned.Root;
        PrologueSector sector = owned.Sector;
        TraversalSegment2D segment = sector.GetNodeOrNull<TraversalSegment2D>(
            $"Gameplay/TraversalContracts/{segmentId}");
        TestAssert.True(segment != null && segment.SegmentId.ToString() == segmentId,
            "owned mandatory legacy fixture admits exact saved teaching segment before mounting");
        CharacterBody2D player = owned.CreatePlayer(new Vector2(
            Mathf.Lerp(segment.LaunchZone.Position.X, segment.LaunchZone.End.X, fraction),
            segment.LaunchZone.GetCenter().Y));
        using var gateTrace = traceGateBoundary ? new LegacyDescentGateBoundaryTrace(fixture, player) : null;
        tree.Root.AddChild(fixture);
        RouteTrace trace = null;
        try
        {
            await WaitForFloor(tree, player);
            if (segmentId == "temple_checkpoint")
                await OpenBellGateForSegment(tree, player, sector);
            trace = new RouteTrace(player.GetNode<PlayerMotor>("Motor"),
                player.GetNode<MechanicalClawController>("MechanicalClaw"));
            if (segment.Kind == TraversalSegmentKind.Assisted)
            {
                GrappleSurface2D assist = sector.GetNode<GrappleSurface2D>(segment.AssistTargetPath);
                MechanicalClawTargetResolution preflight =
                    new MechanicalClawTargetResolver().Resolve(player, assist.WorldAnchor);
                string blocker = preflight.Snapshot.HasLineOfSight
                    ? "clear"
                    : FirstRayHit(player, assist.WorldAnchor);
                TestAssert.True(preflight.Surface == assist && preflight.Snapshot.HasLineOfSight,
                    $"{segment.SegmentId} launch sample has a clear real ray to its exact dedicated " +
                    $"anchor; player={player.GlobalPosition} anchor={assist.WorldAnchor} " +
                    $"resolved={preflight.Surface?.Name ?? "None"} " +
                    $"visible={preflight.Snapshot.HasLineOfSight} blocker={blocker}");
                trace.ExpectedTarget = assist.GetInstanceId();
            }
            trace.Claw.ClawFrameChanged += trace.OnClawFrameChanged;
            bool teachingSample = Mathf.IsEqualApprox(fraction, .5f);
            if (teachingSample && segmentId == "entrance_move_attack")
                await AssertAttackLesson(tree, player);
            if (teachingSample && segmentId == "descent_ice_rejection")
                await AssertIceRejectionLesson(tree, player, sector, trace);
            bool shieldLesson = teachingSample && segmentId == "basin_shield_lane";
            if (shieldLesson)
                await BeginShieldLesson(tree, player);
            if (segment.Kind == TraversalSegmentKind.Assisted)
                await DriveAssisted(tree, player, sector, segment, trace,
                    traceReleaseSampling: segmentId == "ascent_grapple_three" && Mathf.IsEqualApprox(fraction, .2f));
            else
                await DriveSingleJump(tree, player, segment, trace, probeSecondJump: true);
            if (shieldLesson)
            {
                TestAssert.True(player.GetNode<PlayerShieldController>("Shield").IsActive,
                    "basin shield-lane representative crosses while real right-mouse guard is active");
                CompletedLessonActions.Add("shield");
                SendMouse(MouseButton.Right, false, player.GlobalPosition + Vector2.Right * 220f);
                await PhysicsFrames(tree, 2);
            }
            AssertLanding(player, segment, trace);
            owned.AssertNoRecovery();
            GD.Print($"TASK8_ROUTE_SAMPLE_OK sector={sectorCase.Name} segment={segmentId} " +
                $"sample={fraction:0.0} frames={trace.Frames} casts={trace.CastCount} " +
                $"latches={trace.LatchCount} dash={trace.Motor.AcceptedDashCount} " +
                $"second_air_jump={trace.SecondAirJumpAccepted} position={player.Position}");
        }
        finally
        {
            gateTrace?.DumpAndStop();
            ReleaseAll();
            if (trace != null && GodotObject.IsInstanceValid(trace.Claw))
                trace.Claw.ClawFrameChanged -= trace.OnClawFrameChanged;
            fixture.QueueFree();
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            await PhysicsFrames(tree, 2);
        }
    }

    private static async Task AssertAttackLesson(SceneTree tree, CharacterBody2D player)
    {
        PlayerActionModeController modes = player.GetNode<PlayerActionModeController>("ActionModes");
        PlayerCombat combat = player.GetNode<PlayerCombat>("Combat");
        try
        {
            MoveMouse(player.GlobalPosition + new Vector2(220, 0));
            SendAction(InputActions.SelectAttackMode, true);
            await PhysicsFrames(tree, 1);
            SendAction(InputActions.SelectAttackMode, false);
            SendMouse(MouseButton.Left, true, player.GlobalPosition + new Vector2(220, 0));
            await PhysicsFrames(tree, 2);
            TestAssert.True(modes.CurrentMode == PlayerActionMode.Attack && combat.IsAttacking,
                "entrance representative drives real Q plus mouse-left into the melee model; " +
                $"mode={modes.CurrentMode} slot={modes.ActiveSlot} " +
                $"can_attack={modes.CanBeginAttack} attacking={combat.IsAttacking}");
            CompletedLessonActions.Add("attack");
        }
        finally
        {
            SendAction(InputActions.SelectAttackMode, false);
            SendMouse(MouseButton.Left, false, player.GlobalPosition + new Vector2(220, 0));
            combat.ResetTransientState();
            await PhysicsFrames(tree, 2);
        }
    }

    private static async Task AssertIceRejectionLesson(SceneTree tree, CharacterBody2D player,
        PrologueSector sector, RouteTrace trace)
    {
        GrappleSurface2D ice = sector.GetNode<GrappleSurface2D>(
            "Gameplay/GrappleSurfaces/VisibleIceRejection");
        try
        {
            MoveMouse(ice.WorldAnchor);
            SendAction(InputActions.SelectGrappleMode, true);
            await PhysicsFrames(tree, 1);
            SendAction(InputActions.SelectGrappleMode, false);
            SendMouse(MouseButton.Left, true, ice.WorldAnchor);
            await PhysicsFrames(tree, 1);
            SendMouse(MouseButton.Left, false, ice.WorldAnchor);
            for (int frame = 0; frame < 30 && trace.ObservedFailure != MechanicalClawFailure.IceSlip;
                 frame++)
            {
                MoveMouse(ice.WorldAnchor);
                await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
            }
            TestAssert.Equal(MechanicalClawFailure.IceSlip, trace.ObservedFailure,
                "descent representative drives real E plus mouse-left into visible Ice rejection");
            TestAssert.Equal(0, trace.LatchCount, "Ice rejection cannot create a surface latch");
            CompletedLessonActions.Add("ice");
        }
        finally
        {
            SendAction(InputActions.SelectGrappleMode, false);
            SendMouse(MouseButton.Left, false, ice.WorldAnchor);
            await PhysicsFrames(tree, 2);
        }
    }

    private static async Task BeginShieldLesson(SceneTree tree, CharacterBody2D player)
    {
        Vector2 aim = player.GlobalPosition + Vector2.Right * 220f;
        MoveMouse(aim);
        SendMouse(MouseButton.Right, true, aim);
        await PhysicsFrames(tree, 3);
        TestAssert.True(player.GetNode<PlayerShieldController>("Shield").IsActive,
            "basin representative turns on the real right-mouse shield before traversal");
    }

    private static async Task DriveSingleJump(SceneTree tree, CharacterBody2D player,
        TraversalSegment2D segment, RouteTrace trace, bool probeSecondJump)
    {
        float direction = Mathf.Sign(segment.LandingZone.GetCenter().X -
            segment.LaunchZone.GetCenter().X);
        StringName horizontal = direction < 0 ? InputActions.MoveLeft : InputActions.MoveRight;
        SendAction(horizontal, true);
        SendAction(InputActions.Jump, true);
        for (int frame = 0; frame < SingleJumpBudget; frame++)
        {
            trace.Frames = frame;
            if (frame == 8) SendAction(InputActions.Jump, false);
            if (probeSecondJump && frame == 11)
            {
                trace.SecondJumpVelocityBefore = player.Velocity.Y;
                trace.SecondJumpAcceptedCountBefore = trace.Motor.AcceptedJumpCount;
                trace.SecondJumpAttempted = true;
                SendAction(InputActions.Jump, true);
            }
            if (probeSecondJump && frame == 12)
            {
                trace.SecondAirJumpAccepted = trace.Motor.AcceptedJumpCount >
                    trace.SecondJumpAcceptedCountBefore;
                trace.SecondJumpMeasured = true;
                SendAction(InputActions.Jump, false);
            }
            await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
            trace.Observe(player, segment);
            if (direction > 0 && player.Position.X >= segment.LandingZone.GetCenter().X ||
                direction < 0 && player.Position.X <= segment.LandingZone.GetCenter().X)
                SendAction(horizontal, false);
            if (trace.StableLandingFrames >= StableLandingRequirement &&
                HasLandingMargin(segment.LandingZone, player.Position)) break;
        }
        SendAction(InputActions.Jump, false);
        SendAction(horizontal, false);
    }

    private static async Task DriveAssisted(SceneTree tree, CharacterBody2D player,
        PrologueSector sector, TraversalSegment2D segment, RouteTrace trace,
        GrappleSurface2D targetOverride = null, bool boostImmediately = false,
        Vector2? aimOverride = null, bool useCurrentPositionForDirection = false,
        bool traceReleaseSampling = false)
    {
        GrappleSurface2D target = targetOverride ??
            sector.GetNode<GrappleSurface2D>(segment.AssistTargetPath);
        var landing = ReadLegacyAssistedLanding(player, sector, segment);
        trace.ExpectedLandingSupport = landing;
        using var releaseSampling = traceReleaseSampling
            ? new LegacyAscentReleaseBoundaryTrace(player.GetParent(), player, sector, trace) : null;
        using var releaseCorridor = targetOverride == null && segment.SegmentId == "ascent_grapple_three"
            ? new LegacyBoostCorridor(player, trace.Motor, landing.Body, landing.StandingY,
                landing.Left, landing.Right, segment.LandingZone) : null;
        int printedCorridorRejects = 0;
        Vector2 aimWorld = aimOverride ?? target.WorldAnchor;
        float direction = Mathf.Sign(segment.LandingZone.GetCenter().X -
            (useCurrentPositionForDirection ? player.Position.X : segment.LaunchZone.GetCenter().X));
        StringName horizontal = direction < 0 ? InputActions.MoveLeft : InputActions.MoveRight;
        List<string> assistedSamples = Array.Exists(OS.GetCmdlineUserArgs(),
            value => value == "--trace-assisted") ? new() : null;
        int diagnosticFrame = -1;
        int preparationFrames = 0;
        int releaseObservations = 0;
        bool traceReleaseBoundary = Array.Exists(OS.GetCmdlineUserArgs(),
            value => value == "--phase=legacy-ascent-high");
        if (!boostImmediately)
        {
            SendAction(horizontal, true);
            // The exact .2 launch starts from rest. Jumping immediately bends
            // its locked ray into the saved Counterweight Area before latch.
            // Use the same authored launch zone for a real grounded run-up;
            // retain other-target occlusion, the exact anchor and total budget.
            // Catch-return callers supply their own target and launch shelf;
            // those already-passing routes must not enter this lesson run-up.
            if (targetOverride == null && segment.SegmentId == "descent_grip_lesson" && direction > 0)
            {
                Vector2 start = player.Position;
                var entry = sector.GetNode<StaticBody2D>("Gameplay/Collision/EntryShelf");
                Rect2 entryBounds = ShieldLaneRectangle(entry);
                var bodyCapsule = ShieldLaneCapsule(player);
                bool OnOriginalEntry()
                {
                    if (!player.IsOnFloor() || player.Position.X - bodyCapsule.Radius < entryBounds.Position.X ||
                        player.Position.X + bodyCapsule.Radius > entryBounds.End.X ||
                        Mathf.Abs(player.Position.Y + bodyCapsule.HalfHeight - entryBounds.Position.Y) > 1f) return false;
                    for (int index = 0; index < player.GetSlideCollisionCount(); index++)
                    {
                        KinematicCollision2D contact = player.GetSlideCollision(index);
                        if (contact.GetCollider() == entry && contact.GetNormal().Y < -.99f) return true;
                    }
                    return false;
                }
                while (player.Position.X < segment.LaunchZone.GetCenter().X &&
                    preparationFrames < AssistedBudget)
                {
                    TestAssert.True(OnOriginalEntry() && segment.LaunchZone.HasPoint(player.Position),
                        "legacy descent run-up stays supported inside its unchanged launch zone");
                    await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
                    preparationFrames++;
                    trace.Observe(player, segment);
                }
                TestAssert.True(OnOriginalEntry() && segment.LaunchZone.HasPoint(player.Position) &&
                    preparationFrames < AssistedBudget,
                    "legacy descent grounded run-up cannot consume extra time or leave its authored launch zone");
                if (preparationFrames > 0)
                    GD.Print($"TASK8_DESCENT_RUNUP start={start} end={player.Position} frames={preparationFrames} " +
                        $"velocity={player.Velocity} original_budget={AssistedBudget} budget_consumed=true geometry_changed=false " +
                        $"support={entry.Name}/{entry.GetInstanceId()} native_up_contact=true full_capsule=true birth_fraction_not_jump_fraction=true");
            }
            TraceAssisted("ordinary_space_request");
            SendAction(InputActions.Jump, true);
        }
        MoveMouse(aimWorld);
        SendAction(InputActions.SelectGrappleMode, true);
        await PhysicsFrames(tree, 1);
        SendAction(InputActions.SelectGrappleMode, false);
        GD.Print($"TASK3_ASSIST_CAST_DIRECT_RAY segment={segment.SegmentId} " +
            $"from={player.GlobalPosition} target={aimWorld} " +
            $"first_hit={FirstRayHit(player, aimWorld)}");
        SendMouse(MouseButton.Left, true, aimWorld);
        await PhysicsFrames(tree, 1);
        SendMouse(MouseButton.Left, false, aimWorld);
        try
        {
            bool obstructionPrinted = false;
            bool terrainTraversalStarted = false;
            bool terrainAttachmentLossPrinted = false;
            Vector2 terrainApex = player.Position;
            Vector2 terrainApexVelocity = player.Velocity;
            float terrainApexPredictedX = float.NaN;
            float terrainApexRope = 0f;
            Vector2 terrainLowest = player.Position;
            Vector2 terrainLowestVelocity = player.Velocity;
            float terrainLowestRope = 0f;
            int terrainLowestFrame = -1;
            float closestPredictedDistance = float.PositiveInfinity;
            float closestPredictedX = float.NaN;
            Vector2 closestPredictedPosition = player.Position;
            Vector2 closestPredictedVelocity = player.Velocity;
            float closestPredictedRope = 0f;
            int closestPredictedFrame = -1;
            int frameBudget = (boostImmediately ? 420 : AssistedBudget) - preparationFrames;
            for (int frame = 0; frame < frameBudget; frame++)
            {
                diagnosticFrame = frame;
                trace.Frames = preparationFrames + frame;
                MoveMouse(aimWorld);
                await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
                trace.Observe(player, segment);
                TraceAssisted("physics_sample");
                if (traceReleaseBoundary && trace.SecondJumpAttempted && releaseObservations++ < 24)
                    GD.Print($"TASK8_HIGH_RELEASE_OBSERVATION frame={frame} physics={Engine.GetPhysicsFrames()} " +
                        $"position={player.Position} velocity={player.Velocity} state={trace.Claw.CurrentFrame.State} " +
                        $"release={trace.Claw.CurrentFrame.ReleaseKind} sampled_space={player.GetNode<PlayerInputReader>("Input").CurrentFrame.JumpPressed} " +
                        $"release_momentum={trace.Motor.HasGrappleReleaseMomentum} floor={player.IsOnFloor()} contact={FirstSlideHit(player)}");
                if (boostImmediately && terrainTraversalStarted &&
                    !terrainAttachmentLossPrinted &&
                    !IsAttached(trace.Claw.CurrentFrame.State) &&
                    !trace.ReleasedFromAttachedSecondSpace)
                {
                    terrainAttachmentLossPrinted = true;
                    GD.Print($"TASK3_CATCH_RETURN_UNPLANNED_DETACH segment={segment.SegmentId} " +
                        $"frame={frame} position={player.Position} velocity={player.Velocity} " +
                        $"state={trace.Claw.CurrentFrame.State} " +
                        $"failure={trace.Claw.CurrentFrame.Failure} " +
                        $"primary_released={!Input.IsMouseButtonPressed(MouseButton.Left)} " +
                        $"slide_hit={FirstSlideHit(player)}");
                }
                if (boostImmediately && player.Position.Y > terrainLowest.Y)
                {
                    terrainLowest = player.Position;
                    terrainLowestVelocity = player.Velocity;
                    terrainLowestRope = trace.Claw.CurrentFrame.RopeLength;
                    terrainLowestFrame = frame;
                }
                if (!obstructionPrinted && trace.ObservedFailure == MechanicalClawFailure.Obstructed)
                {
                    obstructionPrinted = true;
                    GD.Print($"TASK3_ASSIST_OBSTRUCTION_DIRECT_RAY segment={segment.SegmentId} " +
                        $"frame={frame} cast={trace.Claw.CurrentFrame.CastId} " +
                        $"state={trace.Claw.CurrentFrame.State} from={player.GlobalPosition} " +
                        $"locked_anchor={trace.Claw.CurrentFrame.AnchorPoint} " +
                        $"first_hit={FirstRayHit(player, trace.Claw.CurrentFrame.AnchorPoint)}");
                }
                if (boostImmediately && !terrainTraversalStarted &&
                    IsAttached(trace.Claw.CurrentFrame.State))
                {
                    terrainTraversalStarted = true;
                    SendAction(horizontal, true);
                }
                if (frame == 8) SendAction(InputActions.Jump, false);
                if (trace.SecondJumpAttempted && !trace.SecondJumpMeasured)
                {
                    trace.SecondAirJumpAccepted = trace.Motor.AcceptedJumpCount >
                        trace.SecondJumpAcceptedCountBefore;
                    trace.SecondJumpMeasured = true;
                    releaseSampling?.Mark("before-original-space-release");
                    SendAction(InputActions.Jump, false);
                    releaseSampling?.Mark("after-original-space-release");
                    await PhysicsFrames(tree, 1);
                    releaseSampling?.Mark("after-original-measurement-await");
                    trace.CompleteAttachedSecondSpaceCausality();
                    TraceAssisted("second_space_measured");
                    if (boostImmediately)
                        GD.Print($"TASK3_CATCH_RETURN_BOOST_FRAME segment={segment.SegmentId} " +
                            $"position={player.Position} velocity={player.Velocity} " +
                            $"input={horizontal} state={trace.Claw.CurrentFrame.State} " +
                            $"correction={trace.Claw.CurrentFrame.VelocityCorrection} " +
                            $"rope={trace.Claw.CurrentFrame.RopeLength:0.00} " +
                            $"slide_hit={FirstSlideHit(player)}");
                }
                float safeLandingStart = segment.LandingZone.Position.X +
                    segment.LandingZone.Size.X * LandingMarginRatio;
                float safeLandingEnd = segment.LandingZone.End.X -
                    segment.LandingZone.Size.X * LandingMarginRatio;
                float sampledSwingAxis = player.GetNode<PlayerInputReader>("Input").CurrentFrame.MoveAxis;
                int forecastSteps = Math.Max(0, frameBudget - frame - 1);
                float predictedBoostLandingX = IsAttached(trace.Claw.CurrentFrame.State)
                    ? PredictBoostLandingX(player, trace.Motor, trace.Claw.CurrentFrame,
                        sampledSwingAxis, landing.StandingY, direction, segment.LandingZone.GetCenter().X, forecastSteps,
                        releaseCorridor)
                    : float.NaN;
                if (releaseCorridor is { LastBlockedId: not 0 } && printedCorridorRejects++ < 12)
                    GD.Print($"LEGACY_BOOST_CORRIDOR_REJECT physics={Engine.GetPhysicsFrames()} driver_frame={frame} " +
                        $"future_step={releaseCorridor.LastBlockedStep} collider={releaseCorridor.LastBlockedId} " +
                        $"from={releaseCorridor.LastFrom} motion={releaseCorridor.LastMotion} normal={releaseCorridor.LastNormal} " +
                        "body_not_moved=true original_budget=true");
                if (!trace.SecondJumpAttempted && !float.IsNaN(predictedBoostLandingX))
                {
                    float distance = predictedBoostLandingX < safeLandingStart ?
                        safeLandingStart - predictedBoostLandingX :
                        predictedBoostLandingX > safeLandingEnd ?
                            predictedBoostLandingX - safeLandingEnd : 0f;
                    if (distance < closestPredictedDistance)
                    {
                        closestPredictedDistance = distance;
                        closestPredictedX = predictedBoostLandingX;
                        closestPredictedPosition = player.Position;
                        closestPredictedVelocity = player.Velocity;
                        closestPredictedRope = trace.Claw.CurrentFrame.RopeLength;
                        closestPredictedFrame = frame;
                    }
                }
                // The release forecast keeps the player over authored support. The existing
                // ground correction then moves a supported edge landing into the unchanged
                // 15%-inset final zone.
                bool boostReleaseWindow = predictedBoostLandingX >= landing.Left &&
                    predictedBoostLandingX <= landing.Right ||
                    releaseCorridor is { HasWalkableArrival: true, GroundCorrectionAdmitted: true };
                if (boostImmediately && player.Position.Y < terrainApex.Y)
                {
                    terrainApex = player.Position;
                    terrainApexVelocity = player.Velocity;
                    terrainApexPredictedX = PredictLandingX(player, trace.Motor,
                        landing.StandingY, direction, segment.LandingZone.GetCenter().X, forecastSteps);
                    terrainApexRope = trace.Claw.CurrentFrame.RopeLength;
                }
                if (!trace.SecondJumpAttempted && boostReleaseWindow &&
                    trace.TryBeginAttachedSecondSpace(player, target))
                {
                    TestAssert.True(!player.IsOnFloor(),
                        $"{segment.SegmentId} is airborne immediately before its real second Space edge");
                    trace.SecondJumpAttempted = true;
                    trace.SecondJumpVelocityBefore = player.Velocity.Y;
                    trace.SecondJumpAcceptedCountBefore = trace.Motor.AcceptedJumpCount;
                    GD.Print($"TASK8_ASSIST_RELEASE_FORECAST segment={segment.SegmentId} physics={Engine.GetPhysicsFrames()} " +
                        $"position={player.Position} velocity={player.Velocity} predicted_x={predictedBoostLandingX:R} " +
                        $"standing_y={landing.StandingY:R} full_capsule_left={landing.Left:R} full_capsule_right={landing.Right:R} " +
                        $"support={landing.Body.Name} actual_motion_module=true");
                    if (releaseCorridor != null)
                        GD.Print($"LEGACY_BOOST_CORRIDOR_ADMISSION contact={releaseCorridor.ArrivalPosition} " +
                            $"normal={releaseCorridor.ArrivalNormal} partial={releaseCorridor.ArrivalPosition.X < landing.Left} " +
                            $"ground_preview={releaseCorridor.GroundCorrectionAdmitted} " +
                            $"ground_frames={releaseCorridor.GroundCorrectionFrames} " +
                            $"full_preview_x={releaseCorridor.GroundCompletionX:R} " +
                            "final_native_12_and_margin_still_required=true prediction_not_body_write=true");
                    TraceAssisted("second_space_request");
                    releaseSampling?.Arm(frame, predictedBoostLandingX, forecastSteps,
                        landing.StandingY, landing.Left, landing.Right);
                    SendAction(InputActions.Jump, true);
                    trace.RecordSecondSpaceInputDispatched();
                    releaseSampling?.Mark("after-original-space-dispatch");
                }
                bool reachedLandingCenter =
                    direction > 0 && player.Position.X >= segment.LandingZone.GetCenter().X ||
                    direction < 0 && player.Position.X <= segment.LandingZone.GetCenter().X;
                // A higher catch shelf can share the target's center X. Continue walking
                // off that shelf. On the target-height support, correct any braking drift
                // outside the inset using real A/D rather than accepting a marginal stop.
                float safeLandingTop = segment.LandingZone.Position.Y +
                    segment.LandingZone.Size.Y * LandingMarginRatio;
                float safeLandingBottom = segment.LandingZone.End.Y -
                    segment.LandingZone.Size.Y * LandingMarginRatio;
                bool onLandingSupport = trace.ReleasedFromAttachedSecondSpace &&
                    player.IsOnFloor() && player.Position.Y >= safeLandingTop &&
                    player.Position.Y <= safeLandingBottom &&
                    player.Position.X >= segment.LandingZone.Position.X &&
                    player.Position.X <= segment.LandingZone.End.X;
                if (onLandingSupport)
                {
                    bool correctLeft = player.Position.X > safeLandingEnd;
                    bool correctRight = player.Position.X < safeLandingStart;
                    if (correctLeft || correctRight)
                        TraceAssisted(correctLeft ? "ground_correction_left_request" : "ground_correction_right_request");
                    SendAction(InputActions.MoveLeft, correctLeft);
                    SendAction(InputActions.MoveRight, correctRight);
                }
                else if (trace.ReleasedFromAttachedSecondSpace && reachedLandingCenter)
                    SendAction(horizontal, false);
                if (trace.StableLandingFrames >= StableLandingRequirement &&
                    HasLandingMargin(segment.LandingZone, player.Position)) break;
            }
            GD.Print($"TASK3_CATCH_RETURN_CLOSEST_WINDOW segment={segment.SegmentId} " +
                $"frame={closestPredictedFrame} position={closestPredictedPosition} " +
                $"velocity={closestPredictedVelocity} predicted_x={closestPredictedX:0.00} " +
                $"safe_distance={closestPredictedDistance:0.00} " +
                $"rope={closestPredictedRope:0.00} state={trace.Claw.CurrentFrame.State} " +
                $"dock={trace.Claw.CurrentFrame.DockPoint} normal={trace.Claw.CurrentFrame.SurfaceNormal}");
            if (boostImmediately)
            {
                GD.Print($"TASK3_CATCH_RETURN_APEX segment={segment.SegmentId} " +
                    $"position={terrainApex} velocity={terrainApexVelocity} " +
                    $"predicted_x={terrainApexPredictedX:0.00} rope={terrainApexRope:0.00}");
                GD.Print($"TASK3_CATCH_RETURN_LOWEST segment={segment.SegmentId} " +
                    $"frame={terrainLowestFrame} position={terrainLowest} " +
                    $"velocity={terrainLowestVelocity} rope={terrainLowestRope:0.00}");
            }
        }
        finally
        {
            releaseSampling?.DumpAndStop();
            if (releaseCorridor != null)
                GD.Print($"LEGACY_BOOST_CORRIDOR_END queries={releaseCorridor.Queries} " +
                    $"candidates={releaseCorridor.Candidates} blocked={releaseCorridor.BlockedCandidates} " +
                    "original_240=true original_support=true body_writes=0 input_writes=0 normal_route=false");
            TraceAssisted("route_exit");
            SendAction(InputActions.Jump, false);
            SendMouse(MouseButton.Left, false, aimWorld);
            SendAction(InputActions.SelectGrappleMode, false);
            SendAction(InputActions.MoveUp, false);
            SendAction(InputActions.MoveDown, false);
            SendAction(InputActions.MoveLeft, false);
            SendAction(InputActions.MoveRight, false);
            if (assistedSamples != null)
                foreach (string sample in assistedSamples) GD.Print(sample);
        }

        void TraceAssisted(string moment)
        {
            if (assistedSamples == null) return;
            PlayerInputReader reader = player.GetNode<PlayerInputReader>("Input");
            PlayerInputFrame input = reader.CurrentFrame;
            MechanicalClawFrame clawFrame = trace.Claw.CurrentFrame;
            assistedSamples.Add($"ASSISTED_TRACE segment={segment.SegmentId} fixture={player.GetParent().Name} " +
                $"moment={moment} frame={diagnosticFrame} physics={Engine.GetPhysicsFrames()} " +
                $"sample={reader.CurrentFrameSequence} position={player.Position} velocity={player.Velocity} " +
                $"floor={player.IsOnFloor()} stable={trace.StableLandingFrames} jumps={trace.Motor.AcceptedJumpCount} " +
                $"move={input.MoveAxis} aim={input.Aim} jump={input.JumpPressed}/{input.JumpHeld} " +
                $"primary={input.PrimaryPressed}/{input.PrimaryHeld}/{input.PrimaryReleased} " +
                $"state={clawFrame.State} failure={clawFrame.Failure} cast={clawFrame.CastId} " +
                $"target={trace.Claw.CurrentTarget?.Name.ToString() ?? "None"} rope={clawFrame.RopeLength:0.00} " +
                $"correction={clawFrame.VelocityCorrection} boost={trace.BoostFromSecondSpaceObserved} " +
                $"space_release={trace.ReleasedFromAttachedSecondSpace} " +
                $"predicted_x={PredictLandingX(player, trace.Motor, landing.StandingY, direction, segment.LandingZone.GetCenter().X, Math.Max(0, (boostImmediately ? 420 : AssistedBudget) - preparationFrames - Math.Max(0, diagnosticFrame))):0.00} " +
                $"predicted_boost_x={(IsAttached(clawFrame.State) ? PredictBoostLandingX(player, trace.Motor, clawFrame, input.MoveAxis, landing.StandingY, direction, segment.LandingZone.GetCenter().X, Math.Max(0, (boostImmediately ? 420 : AssistedBudget) - preparationFrames - Math.Max(0, diagnosticFrame))) : float.NaN):0.00} " +
                $"slide_hit={FirstSlideHit(player)}");
        }
    }

    // Break caught: a V1 sector-limit oracle or lost native support can hide V2 capsule clipping
    // at the y=3600 Basin/Ascent seam. These are grounded holds, not InputMap seam traversal.
    private static async Task AssertBasinAscentSeamCameraProtectsWholeCapsule(SceneTree tree)
    {
        Vector2I oldSize = tree.Root.Size;
        Vector2I oldContentSize = tree.Root.ContentScaleSize;
        try
        {
            tree.Root.Size = new Vector2I(1280, 720);
            tree.Root.ContentScaleSize = new Vector2I(1280, 720);
            foreach ((PrologueSectorId sector, Vector2 spawn, Vector2 settled,
                         string supportName, Rect2 supportBounds, bool aboveSharedBand) in new[]
                     {
                         (PrologueSectorId.CanyonBasin, new Vector2(3915, 3759), new Vector2(3915, 3760),
                             "ShortcutStep3796", new Rect2(3840, 3796, 150, 32), false),
                         (PrologueSectorId.CanyonAscent, new Vector2(3915, 3547), new Vector2(3915, 3548),
                             "LiftEntryFloor", new Rect2(3840, 3584, 160, 16), false),
                         (PrologueSectorId.CanyonAscent, new Vector2(4780, 1283), new Vector2(4780, 1284),
                             "AscentFinalCatch", new Rect2(4660, 1320, 240, 40), true)
                     })
            {
                PrologueLevelController level = CreateCameraSeamFixture(sector, spawn);
                CharacterBody2D player = level.GetNode<CharacterBody2D>("PlayerRoot/Player");
                SeamlessPrologueWorld world = level.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors");
                int recoveries = 0;
                void Recovered(string _) => recoveries++;
                level.RecoveryCompleted += Recovered;
                ulong playerId = player.GetInstanceId();
                ulong worldId = world.GetInstanceId();
                tree.Root.AddChild(level);
                try
                {
                    await PhysicsFrames(tree, 8);
                    await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
                    Camera2D camera = player.GetNode<Camera2D>("Camera2D");
                    CollisionShape2D collider = player.GetNode<CollisionShape2D>("BodyCollider");
                    TestAssert.True(collider.Shape is CapsuleShape2D,
                        "seam camera regression projects the actual player capsule");
                    StaticBody2D support = BasinAscentCameraRuntimeTests.ResolveAuthoredSupport(
                        world, sector, supportName, supportBounds);
                    Vector2 initialOffset = camera.Offset;
                    for (int sample = 0; sample < 12; sample++)
                    {
                        await PhysicsFrames(tree, 1);
                        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
                        TestAssert.Equal(sector, world.CurrentSector,
                            "camera protection cannot change seamless-world sector authority");
                        TestAssert.Equal(sector, level.CurrentSector!.Value,
                            "level location retains the same authoritative sector");
                        TestAssert.True(world.MountedSectorIds.Contains(PrologueSectorId.CanyonBasin) &&
                            world.MountedSectorIds.Contains(PrologueSectorId.CanyonAscent) &&
                            world.MountedSectorIds.Count <= 3,
                            "camera protection uses only the already-mounted stream set");
                        TestAssert.Equal(playerId, level.Player.GetInstanceId(),
                            "camera protection preserves the persistent player");
                        TestAssert.Equal(worldId, level.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors").GetInstanceId(),
                            "camera protection preserves the persistent seamless world");
                        TestAssert.Equal(0, recoveries,
                            "camera protection cannot relocate through recovery");
                        float margin = BasinAscentCameraRuntimeTests.AssertV2GroundedSample(player, world,
                            support, sector, settled, aboveSharedBand, initialOffset);
                        GD.Print($"TASK7_V2_GROUNDED_CAMERA_SAMPLE sector={sector} high={aboveSharedBand} " +
                            $"sample={sample} support={support.GetPath()} native_upward_contact=True margin={margin}");
                    }
                }
                finally
                {
                    level.RecoveryCompleted -= Recovered;
                    await TestSceneTeardown.FreeSceneAfterAudioDrain(tree, level);
                }
            }
        }
        finally
        {
            tree.Root.Size = oldSize;
            tree.Root.ContentScaleSize = oldContentSize;
        }
    }

    // Controlled pre-tree V2 hold. Input is disabled; native motor and camera remain automatic.
    internal static PrologueLevelController CreateCameraSeamFixture(PrologueSectorId sector,
        Vector2 spawn)
    {
        PrologueLevelController level = GD.Load<PackedScene>(
                "res://scenes/prologue/PrologueLevel.tscn")
            .Instantiate<PrologueLevelController>();
        CharacterBody2D player = level.GetNode<CharacterBody2D>("PlayerRoot/Player");
        player.Position = spawn;
        player.GetNode<PlayerInputReader>("Input").ProcessMode = Node.ProcessModeEnum.Disabled;
        level.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors").InitialSector = sector;
        PrologueGameFlowController flow = level.GetNode<PrologueGameFlowController>("GameFlow");
        flow.HeadlessAutoStart = false;
        flow.AutomationAutoStart = true;
        IsolatedPrologueFixture.Configure(level, "task7-camera");
        return level;
    }

    internal static PrologueLevelController CreateCameraTransitionFixture()
    {
        PrologueLevelController level = GD.Load<PackedScene>(
                "res://scenes/prologue/PrologueLevel.tscn")
            .Instantiate<PrologueLevelController>();
        CharacterBody2D player = level.GetNode<CharacterBody2D>("PlayerRoot/Player");
        player.Position = new Vector2(2892.43f, 1850f);
        player.GetNode<PlayerInputReader>("Input").ProcessMode = Node.ProcessModeEnum.Disabled;
        player.GetNode<PlayerMotor>("Motor").ProcessMode = Node.ProcessModeEnum.Disabled;
        level.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors").InitialSector = PrologueSectorId.CanyonAscent;
        PrologueGameFlowController flow = level.GetNode<PrologueGameFlowController>("GameFlow");
        flow.HeadlessAutoStart = false;
        flow.AutomationAutoStart = true;
        IsolatedPrologueFixture.Configure(level, "task7-camera-transition");
        return level;
    }

    // V1-only historical x-contraction oracle. Deliberately not called by the saved V2 grounded
    // suite; CreateCameraTransitionFixture is retained for existing off-tree storage-audit callers.
    private static async Task AssertV1AscentCameraContractsOnTheFirstUncoveredFollowFrame(SceneTree tree)
    {
        Vector2I oldSize = tree.Root.Size;
        Vector2I oldContentSize = tree.Root.ContentScaleSize;
        tree.Root.Size = new Vector2I(1280, 720);
        tree.Root.ContentScaleSize = new Vector2I(1280, 720);
        PrologueLevelController level = CreateCameraTransitionFixture();
        CharacterBody2D player = level.GetNode<CharacterBody2D>("PlayerRoot/Player");
        PlayerInputReader input = player.GetNode<PlayerInputReader>("Input");
        PlayerMotor motor = player.GetNode<PlayerMotor>("Motor");
        SeamlessPrologueWorld world = level.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors");
        int recoveries = 0;
        void Recovered(string _) => recoveries++;
        level.RecoveryCompleted += Recovered;
        ulong playerId = player.GetInstanceId();
        tree.Root.AddChild(level);
        try
        {
            await PhysicsFrames(tree, 2);
            Camera2D camera = player.GetNode<Camera2D>("Camera2D");
            TestAssert.Equal(new Vector2(1280, 720), camera.GetViewportRect().Size,
                "vertical transition regression uses the real logical viewport");
            Vector2 initialOffset = camera.Offset;
            player.Velocity = Vector2.Zero;
            motor.ApplyGrappleRelease(Vector2.Left, Vector2.Zero,
                MechanicalClawSettings.Default.ReleaseSpeedLimit, preserveLaunchMomentum: true);
            for (int frame = 0; frame < 90 && camera.LimitLeft >= 2880; frame++)
            {
                await PhysicsFrames(tree, 1);
                await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            }
            TestAssert.True(camera.LimitLeft < 2880,
                "controlled release follow first reaches the complete Basin/Ascent shared band");

            Input.ActionPress(InputActions.Jump);
            Input.FlushBufferedEvents();
            input._PhysicsProcess(0d);
            input.ConsumeOneShotEdgesForCurrentPhysicsFrame();
            int acceptedJumps = motor.AcceptedJumpCount;
            motor.ProcessMode = Node.ProcessModeEnum.Inherit;
            motor.ApplyGrappleRelease(Vector2.Left,
                Vector2.Up * MechanicalClawSettings.Default.BoostUpwardSpeed,
                MechanicalClawSettings.Default.ReleaseSpeedLimit, preserveLaunchMomentum: true);
            bool crossedAboveCoverage = false;
            float minimumVisibleTop = float.PositiveInfinity;
            float startPlayerY = player.GlobalPosition.Y;
            float minimumPlayerY = startPlayerY;
            for (int frame = 0; frame < 90 && !crossedAboveCoverage; frame++)
            {
                await PhysicsFrames(tree, 1);
                await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
                minimumPlayerY = Mathf.Min(minimumPlayerY, player.GlobalPosition.Y);
                float visibleTop = camera.GetScreenCenterPosition().Y -
                    camera.GetViewportRect().Size.Y * .5f / camera.Zoom.Y;
                minimumVisibleTop = Mathf.Min(minimumVisibleTop, visibleTop);
                if (visibleTop >= 1440f - .01f) continue;
                crossedAboveCoverage = true;
                TestAssert.True(camera.LimitLeft >= 2880,
                    "the first post-follow frame above Basin coverage contracts to Ascent before rendering; " +
                    $"visible_top={visibleTop} limit_left={camera.LimitLeft}");
                TestAssert.Equal(initialOffset, camera.Offset,
                    "vertical transition protection preserves camera shake Offset");
                TestAssert.Equal(PrologueSectorId.CanyonAscent, world.CurrentSector,
                    "vertical camera follow cannot change seamless-world authority");
                TestAssert.Equal(PrologueSectorId.CanyonAscent, level.CurrentSector!.Value,
                    "vertical camera follow preserves the level location sector");
                TestAssert.True(world.MountedSectorIds.Contains(PrologueSectorId.CanyonBasin) &&
                    world.MountedSectorIds.Contains(PrologueSectorId.CanyonAscent) &&
                    world.MountedSectorIds.Count <= 3,
                    "vertical camera follow uses only the existing mounted stream set");
                TestAssert.Equal(playerId, level.Player.GetInstanceId(),
                    "vertical camera follow preserves the persistent player");
                TestAssert.Equal(0, recoveries,
                    "vertical camera follow cannot relocate through recovery");
            }
            TestAssert.True(crossedAboveCoverage,
                "controlled Ascent follow crosses above Basin's complete vertical coverage band; " +
                $"min_visible_top={minimumVisibleTop} camera_position={camera.Position} " +
                $"player={player.Position} start_y={startPlayerY} min_y={minimumPlayerY} " +
                $"limit_left={camera.LimitLeft}");
            TestAssert.True(startPlayerY - minimumPlayerY > 1f,
                "vertical transition regression includes actual upward PlayerMotor movement; " +
                $"rise={startPlayerY - minimumPlayerY}");
            TestAssert.Equal(acceptedJumps, motor.AcceptedJumpCount,
                "held Space sustains the real release without injecting an ordinary jump");
            GD.Print("TASK7_CAMERA_VERTICAL_TRANSITION_OK " +
                $"min_visible_top={minimumVisibleTop} limit_left={camera.LimitLeft} " +
                $"player_rise={startPlayerY - minimumPlayerY}");
        }
        finally
        {
            Input.ActionRelease(InputActions.Jump);
            Input.FlushBufferedEvents();
            level.RecoveryCompleted -= Recovered;
            await TestSceneTeardown.FreeSceneAfterAudioDrain(tree, level);
            tree.Root.Size = oldSize;
            tree.Root.ContentScaleSize = oldContentSize;
        }
    }

    private static void AssertLanding(CharacterBody2D player, TraversalSegment2D segment,
        RouteTrace trace)
    {
        TestAssert.True(trace.LeftFloor, $"{segment.SegmentId} leaves its real launch support");
        TestAssert.True(player.IsOnFloor() &&
            trace.StableLandingFrames >= StableLandingRequirement &&
            HasLandingMargin(segment.LandingZone, player.Position),
            $"{segment.SegmentId} lands for twelve stable frames with at least 15% horizontal " +
            "and vertical margin inside the authored safe zone; " +
            $"position={player.Position} frames={trace.Frames} casts={trace.CastCount} " +
            $"latches={trace.LatchCount} attached={trace.HasAttached} " +
            $"expected_target={trace.ExpectedTarget} actual_target={trace.LatchedTarget} " +
            $"latched_anchor={trace.LatchedAnchor} second_attempted={trace.SecondJumpAttempted} " +
            $"boost={trace.BoostFromSecondSpaceObserved} " +
            $"claw_state={trace.Claw.CurrentFrame.State} " +
            $"claw_failure={trace.Claw.CurrentFrame.Failure} observed_failure={trace.ObservedFailure} " +
            $"mode={player.GetNode<PlayerActionModeController>("ActionModes").CurrentMode}");
        TestAssert.Equal(0, trace.Motor.AcceptedDashCount,
            $"{segment.SegmentId} accepts no Dash on the mandatory route");
        TestAssert.True(!trace.SecondAirJumpAccepted,
            $"{segment.SegmentId} rejects a second Space press while airborne");
        TestAssert.True(trace.SecondJumpAttempted && trace.SecondJumpMeasured,
            $"{segment.SegmentId} performs and measures a real InputMap second-air-jump attempt");
        if (segment.Kind == TraversalSegmentKind.Assisted)
        {
            TestAssert.True(trace.AttachedSecondSpaceCausalityObserved,
                $"{segment.SegmentId} proves the current expected cast is attached at the " +
                "Space edge after the click was released, publishes Boost then Released, and " +
                "does not create an ordinary motor jump");
            TestAssert.True(trace.ReleasedFromAttachedSecondSpace,
                $"{segment.SegmentId} ends gameplay attachment in the same Space transaction");
            GD.Print($"TASK8_ATTACHED_SECOND_SPACE_OK segment={segment.SegmentId} " +
                $"airborne_edge={trace.AttachedSecondSpaceAirborneAtEdge} " +
                $"boost={trace.BoostFromSecondSpaceObserved} " +
                $"primary_released={trace.PrimaryReleasedBeforeSecondSpace} " +
                $"space_release={trace.ReleasedFromAttachedSecondSpace}");
            TestAssert.Equal(1, trace.CastCount,
                $"{segment.SegmentId} starts one real mechanical-claw cast");
            TestAssert.Equal(1, trace.LatchCount,
                $"{segment.SegmentId} reaches one real surface latch");
            TestAssert.Equal(trace.ExpectedTarget, trace.LatchedTarget,
                $"{segment.SegmentId} latches its exact authored assist surface");
        }
        else if (segment.SegmentId.ToString() == "descent_ice_rejection")
        {
            if (trace.ObservedFailure == MechanicalClawFailure.IceSlip)
            {
                TestAssert.Equal(0, trace.CastCount,
                    "descent Ice rejection fails before allocating a mechanical-claw cast id");
                TestAssert.Equal(0, trace.LatchCount,
                    "descent Ice lesson rejects rather than latching");
            }
            else
            {
                TestAssert.Equal(0, trace.CastCount,
                    "non-representative Ice route samples remain pure jump probes");
            }
        }
        else
        {
            TestAssert.Equal(0, trace.CastCount,
                $"{segment.SegmentId} jump route does not hide a claw cast");
        }
    }

    // Break caught: rendering a grippable wall while leaving its collision untyped makes the
    // real E + LMB terrain ray miss or lock the decorative center instead of the hit face.
    private static async Task AssertDenseTerrainWallLatch(SceneTree tree)
    {
        ReleaseAll();
        using var owned = new LegacyDescentTerrainFixture("Task3DenseTerrainWallLatch", new Vector2(170f, 574f));
        var fixture = owned.Root;
        PrologueSector sector = owned.Sector;
        StaticBody2D wall = sector.GetNode<StaticBody2D>("Gameplay/Collision/CraneShaftWall");
        GrappleSurface2D profile = wall.GetNode<GrappleSurface2D>("GrappleSurfaceProfile");
        CharacterBody2D player = owned.Player;
        tree.Root.AddChild(fixture);
        MechanicalClawController claw = player.GetNode<MechanicalClawController>("MechanicalClaw");
        var trace = new RouteTrace(player.GetNode<PlayerMotor>("Motor"), claw)
            { ExpectedTarget = profile.GetInstanceId() };
        bool cancellationObserved = false;
        ulong cancellationCast = 0UL;
        void ObserveCancellation(MechanicalClawFrame frame, GrappleSurface2D _)
        {
            if (frame.State == MechanicalClawState.Released &&
                frame.ReleaseKind == MechanicalClawReleaseKind.Cancel)
            {
                cancellationObserved = true;
                cancellationCast = frame.CastId;
            }
        }
        claw.ClawFrameChanged += trace.OnClawFrameChanged;
        claw.ClawFrameChanged += ObserveCancellation;
        Vector2 cursor = wall.Position + new Vector2(30f, 30f);
        RectangleShape2D wallShape = wall.GetNode<CollisionShape2D>("Collision").Shape as
            RectangleShape2D ?? throw new InvalidOperationException("wall must be rectangular");
        float expectedFaceX = wall.Position.X - wallShape.Size.X / 2f;
        try
        {
            await WaitForFloor(tree, player);
            MoveMouse(cursor);
            SendAction(InputActions.SelectGrappleMode, true);
            await PhysicsFrames(tree, 1);
            SendAction(InputActions.SelectGrappleMode, false);
            SendMouse(MouseButton.Left, true, cursor);
            await PhysicsFrames(tree, 1);
            SendMouse(MouseButton.Left, false, cursor);
            for (int frame = 0; frame < 120 && !trace.HasAttached; frame++)
            {
                MoveMouse(cursor);
                await PhysicsFrames(tree, 1);
            }
            TestAssert.True(trace.HasAttached && trace.LatchedTarget == profile.GetInstanceId(),
                "real E + LMB latches the typed CraneShaft wall collision profile");
            TestAssert.True(claw.CurrentFrame.AnchorPoint.DistanceTo(profile.WorldAnchor) > 8f,
                "wall latch preserves the physical hit point instead of snapping to profile center");
            TestAssert.Near(expectedFaceX, claw.CurrentFrame.AnchorPoint.X, 2f,
                "wall latch stays on the first physical collision face rather than the cursor");
            TestAssert.True(claw.CurrentFrame.AnchorPoint.DistanceTo(cursor) > 8f,
                $"wall collision ends the ray before the cursor; cursor={cursor} " +
                $"actual={claw.CurrentFrame.AnchorPoint}");
            float ropeBefore = claw.CurrentFrame.RopeLength;
            SendAction(InputActions.MoveRight, true);
            for (int frame = 0; frame < 24; frame++)
                await PhysicsFrames(tree, 1);
            SendAction(InputActions.MoveRight, false);
            TestAssert.True(trace.HasAttached && claw.CurrentFrame.State == MechanicalClawState.Reel &&
                claw.CurrentFrame.RopeLength < ropeBefore,
                "wall latch auto-pulls while real D supplies only optional tangential steering");
            ulong attachedCast = claw.CurrentFrame.CastId;
            SendMouse(MouseButton.Left, true, cursor);
            await PhysicsFrames(tree, 1);
            SendMouse(MouseButton.Left, false, cursor);
            for (int frame = 0; frame < 6 && !cancellationObserved; frame++)
                await PhysicsFrames(tree, 1);
            TestAssert.True(cancellationObserved && cancellationCast == attachedCast &&
                claw.CurrentFrame.State is MechanicalClawState.Released or
                    MechanicalClawState.Retract or MechanicalClawState.Idle,
                "the wall route publishes a typed second-click cancellation for the attached cast; " +
                $"observed={cancellationObserved} expected_cast={attachedCast} actual_cast={cancellationCast} " +
                $"current={claw.CurrentFrame.State}/{claw.CurrentFrame.ReleaseKind}");
            owned.AssertNoRecovery();
            GD.Print($"LEGACY_DENSE_WALL_OK face_x={claw.CurrentFrame.AnchorPoint.X:R} expected_face={expectedFaceX:R} same_cast_cancel=true pointer_adapter=true v2_main=false");
        }
        finally
        {
            SendMouse(MouseButton.Left, false, cursor);
            if (GodotObject.IsInstanceValid(claw)) claw.ClawFrameChanged -= ObserveCancellation;
            if (GodotObject.IsInstanceValid(claw)) claw.ClawFrameChanged -= trace.OnClawFrameChanged;
            ReleaseAll();
            fixture.QueueFree();
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            await PhysicsFrames(tree, 2);
        }
    }

    // Break caught: a ceiling profile that cannot auto-pull or atomically boost-release
    // would silently restore obsolete held-LMB/W/S route semantics.
    private static async Task AssertCeilingLatchBoostAndReel(SceneTree tree)
    {
        ReleaseAll();
        using var owned = new LegacyDescentTerrainFixture("Task3CeilingBoostReel", new Vector2(720f, 704f));
        var fixture = owned.Root;
        PrologueSector sector = owned.Sector;
        StaticBody2D ceiling = sector.GetNode<StaticBody2D>("Gameplay/Collision/FrostVentCeiling");
        GrappleSurface2D profile = ceiling.GetNode<GrappleSurface2D>("GrappleSurfaceProfile");
        CharacterBody2D player = owned.Player;
        tree.Root.AddChild(fixture);
        MechanicalClawController claw = player.GetNode<MechanicalClawController>("MechanicalClaw");
        PlayerMotor motor = player.GetNode<PlayerMotor>("Motor");
        var releaseSampler = new ReleaseVelocitySampler
        {
            Name = "CeilingReleaseVelocitySampler",
            Player = player,
            ProcessPhysicsPriority = -50
        };
        fixture.AddChild(releaseSampler);
        var states = new List<MechanicalClawFrame>();
        claw.ClawFrameChanged += Observe;
        Vector2 hit = ceiling.Position + new Vector2(-40f, -40f);
        try
        {
            await WaitForFloor(tree, player);
            MoveMouse(hit);
            SendAction(InputActions.SelectGrappleMode, true);
            await PhysicsFrames(tree, 1);
            SendAction(InputActions.SelectGrappleMode, false);
            SendMouse(MouseButton.Left, true, hit);
            await PhysicsFrames(tree, 1);
            SendMouse(MouseButton.Left, false, hit);
            for (int frame = 0; frame < 120 && !IsAttached(claw.CurrentFrame.State); frame++)
            {
                MoveMouse(hit);
                await PhysicsFrames(tree, 1);
            }
            TestAssert.True(IsAttached(claw.CurrentFrame.State) && claw.CurrentTarget == profile,
                $"real ceiling underside ray latches its typed collision profile; " +
                $"state={claw.CurrentFrame.State} failure={claw.CurrentFrame.Failure} " +
                $"anchor={claw.CurrentFrame.AnchorPoint} player={player.GlobalPosition} " +
                $"cursor={hit} target={claw.CurrentTarget?.GetParent()?.Name ?? "None"}");
            ulong cast = claw.CurrentFrame.CastId;
            float ropeBefore = claw.CurrentFrame.RopeLength;
            await PhysicsFrames(tree, 8);
            float reeledIn = claw.CurrentFrame.RopeLength;
            await PhysicsFrames(tree, 8);
            float reeledFurther = claw.CurrentFrame.RopeLength;
            TestAssert.True(reeledIn < ropeBefore && reeledFurther <= reeledIn &&
                states.Any(frame => frame.State == MechanicalClawState.Reel),
                $"automatic take-up shortens the ceiling rope with zero vertical input; " +
                $"before={ropeBefore:0.00} first={reeledIn:0.00} later={reeledFurther:0.00}");
            for (int frame = 0; frame < 120 && claw.CurrentFrame.State != MechanicalClawState.Swing; frame++)
                await PhysicsFrames(tree, 1);
            TestAssert.True(claw.CurrentFrame.State == MechanicalClawState.Swing &&
                claw.CurrentFrame.Arrived,
                "ceiling auto-pull reaches its observable arrived Swing boundary");

            SendAction(InputActions.MoveLeft, true);
            await PhysicsFrames(tree, 3);
            SendAction(InputActions.MoveLeft, false);
            SendAction(InputActions.MoveRight, true);
            int jumpsBefore = motor.AcceptedJumpCount;
            SendAction(InputActions.Jump, true);
            await PhysicsFrames(tree, 1);
            SendAction(InputActions.Jump, false);
            await PhysicsFrames(tree, 1);
            await PhysicsFrames(tree, 1);
            SendAction(InputActions.MoveRight, false);
            int boostIndex = states.FindIndex(frame => frame.State == MechanicalClawState.Boost &&
                frame.CastId == cast);
            TestAssert.True(boostIndex >= 0 && states.Count > boostIndex + 1 &&
                    states[boostIndex + 1].State == MechanicalClawState.Released &&
                    states[boostIndex + 1].CastId == cast &&
                    states[boostIndex + 1].ReleaseKind == MechanicalClawReleaseKind.Boost,
                "attached Space publishes ordered Boost then Released for the exact ceiling cast");
            TestAssert.Equal(jumpsBefore, motor.AcceptedJumpCount,
                "attached ceiling Space does not consume an ordinary motor jump");
            MechanicalClawFrame boostSource = states[boostIndex];
            Vector2 toAnchor = boostSource.CasterWorldPosition.DirectionTo(boostSource.AnchorPoint);
            Vector2 tangent = new(-toAnchor.Y, toAnchor.X);
            Vector2 requestedBoost = Vector2.Up * MechanicalClawSettings.Default.BoostUpwardSpeed +
                tangent * MechanicalClawSettings.Default.BoostTangentialSpeed;
            Vector2 rawRelease = boostSource.CasterVelocity + requestedBoost;
            Vector2 outwardNormal = boostSource.SurfaceNormal.Normalized();
            float inwardRelease = rawRelease.Dot(outwardNormal);
            Vector2 expectedRelease = inwardRelease < 0f
                ? rawRelease - outwardNormal * inwardRelease
                : rawRelease;
            float releaseCap = MechanicalClawSettings.Default.ReleaseSpeedLimit;
            if (expectedRelease.Length() > releaseCap)
                expectedRelease = expectedRelease.Normalized() * releaseCap;
            GD.Print($"TASK7_CEILING_BOOST_SOURCE player={boostSource.CasterWorldPosition} " +
                $"velocity={boostSource.CasterVelocity} anchor={boostSource.AnchorPoint} dock={boostSource.DockPoint} " +
                $"normal={boostSource.SurfaceNormal} requested_boost={requestedBoost} " +
                $"raw_release={rawRelease} inward={inwardRelease:0.00}");
            TestAssert.True(releaseSampler.Captured,
                "ceiling route samples velocity after claw release application and before motor gravity");
            TestAssert.True(releaseSampler.Velocity.DistanceTo(expectedRelease) < .1f,
                "ceiling release velocity applies the independently computed outward-normal projection " +
                $"and existing cap; expected={expectedRelease} actual={releaseSampler.Velocity}");
            TestAssert.True(Math.Abs(releaseSampler.Velocity.X) > 100f,
                $"ceiling release retains meaningful tangential velocity; actual={releaseSampler.Velocity}");
            TestAssert.Equal(0, motor.AcceptedDashCount,
                "ceiling boost/reel probe uses zero Dash");
            owned.AssertNoRecovery();
            GD.Print("LEGACY_CEILING_BOOST_OK grip_stone_not_ice=true arrived_swing=true ordered_boost_release=true original_projection=true ordinary_jump_unchanged=true v2_main=false");
        }
        finally
        {
            SendMouse(MouseButton.Left, false, hit);
            if (GodotObject.IsInstanceValid(claw)) claw.ClawFrameChanged -= Observe;
            ReleaseAll();
            fixture.QueueFree();
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            await PhysicsFrames(tree, 2);
        }

        void Observe(MechanicalClawFrame frame, GrappleSurface2D _)
        {
            states.Add(frame);
            if (frame.State == MechanicalClawState.Boost) releaseSampler.Arm();
        }
    }

    // Break caught: a fall-recovery surface that is visually present but unreachable through the
    // real claw input cannot save the player onto its nearby authored catch layer.
    private static async Task AssertFallingSelfRescue(SceneTree tree)
    {
        ReleaseAll();
        using var owned = new LegacyDescentTerrainFixture("Task3FallingSelfRescue", new Vector2(1000f, 400f));
        var fixture = owned.Root;
        PrologueSector sector = owned.Sector;
        StaticBody2D wall = sector.GetNode<StaticBody2D>("Gameplay/Collision/SelfRescueWall");
        StaticBody2D catchLayer = sector.GetNode<StaticBody2D>("Gameplay/Collision/SelfRescueCatch");
        GrappleSurface2D profile = wall.GetNode<GrappleSurface2D>("GrappleSurfaceProfile");
        CharacterBody2D player = owned.Player;
        tree.Root.AddChild(fixture);
        ActiveInputCanvas = player;
        MechanicalClawController claw = player.GetNode<MechanicalClawController>("MechanicalClaw");
        PlayerInputReader input = player.GetNode<PlayerInputReader>("Input");
        var trace = new RouteTrace(player.GetNode<PlayerMotor>("Motor"), claw)
            { ExpectedTarget = profile.GetInstanceId() };
        claw.ClawFrameChanged += trace.OnClawFrameChanged;
        Vector2 hit = wall.Position;
        try
        {
            await PhysicsFrames(tree, 2);
            TestAssert.True(!player.IsOnFloor(),
                "self-rescue begins from a genuinely falling body without a position reset after mount");
            MechanicalClawTargetResolution preflight =
                new MechanicalClawTargetResolver().Resolve(player, hit);
            TestAssert.True(preflight.Surface == profile,
                $"self-rescue cursor resolves the intended wall before casting; " +
                $"surface={preflight.Surface?.GetParent()?.Name ?? "None"} " +
                $"exists={preflight.Snapshot.Exists} anchor={preflight.Snapshot.WorldAnchor} " +
                $"position={player.Position} cursor={hit}");
            MoveMouse(hit);
            SendAction(InputActions.SelectGrappleMode, true);
            await PhysicsFrames(tree, 1);
            TestAssert.True(input.CurrentFrame.MouseWorld.DistanceTo(hit) <= 1f,
                $"self-rescue real input samples the intended wall cursor; " +
                $"sampled={input.CurrentFrame.MouseWorld} intended={hit}");
            GD.Print($"TASK3_SELF_RESCUE_INPUT_VIEWPORT_OK sampled={input.CurrentFrame.MouseWorld} " +
                $"intended={hit} events={ViewportInputTransport}");
            SendAction(InputActions.SelectGrappleMode, false);
            SendMouse(MouseButton.Left, true, hit);
            await PhysicsFrames(tree, 1);
            SendMouse(MouseButton.Left, false, hit);
            for (int frame = 0; frame < 90 && !trace.HasAttached; frame++)
            {
                MoveMouse(hit);
                await PhysicsFrames(tree, 1);
            }
            TestAssert.True(trace.HasAttached && trace.LatchedTarget == trace.ExpectedTarget &&
                claw.CurrentTarget == profile && !player.IsOnFloor(),
                $"falling player reaches a real typed-terrain latch before touching a catch layer; " +
                $"state={claw.CurrentFrame.State} failure={claw.CurrentFrame.Failure} " +
                $"observed_failure={trace.ObservedFailure} casts={trace.CastCount} " +
                $"latches={trace.LatchCount} position={player.Position} " +
                $"target={claw.CurrentTarget?.GetParent()?.Name ?? "None"}");
            GD.Print($"TASK3_SELF_RESCUE_LATCH position={player.Position} velocity={player.Velocity} " +
                $"anchor={trace.LatchedAnchor} dock={claw.CurrentFrame.DockPoint} " +
                $"normal={claw.CurrentFrame.SurfaceNormal} state={claw.CurrentFrame.State} " +
                $"catch_center={catchLayer.Position}");
            SendAction(InputActions.MoveRight, true);
            await PhysicsFrames(tree, 18);
            GD.Print($"TASK3_SELF_RESCUE_CANCEL position={player.Position} velocity={player.Velocity} " +
                $"anchor={claw.CurrentFrame.AnchorPoint} dock={claw.CurrentFrame.DockPoint} " +
                $"normal={claw.CurrentFrame.SurfaceNormal} state={claw.CurrentFrame.State}");
            SendMouse(MouseButton.Left, true, hit);
            await PhysicsFrames(tree, 1);
            SendMouse(MouseButton.Left, false, hit);
            int stable = 0;
            for (int frame = 0; frame < 240 && stable < StableLandingRequirement; frame++)
            {
                await PhysicsFrames(tree, 1);
                stable = player.IsOnFloor() && IsStandingOn(player, catchLayer) ? stable + 1 : 0;
            }
            SendAction(InputActions.MoveRight, false);
            TestAssert.True(stable >= StableLandingRequirement,
                $"falling self-rescue returns to the nearby named catch layer for twelve frames; " +
                $"position={player.Position} velocity={player.Velocity} " +
                $"catch_center={catchLayer.Position} state={claw.CurrentFrame.State}");
            TestAssert.Equal(0, trace.Motor.AcceptedDashCount,
                "falling self-rescue uses zero Dash");
            owned.AssertNoRecovery();
            GD.Print($"LEGACY_FALLING_RESCUE_OK stable={stable} exact_named_catch=true dash=0 recovery=0 pointer_adapter=true v2_main=false");
        }
        finally
        {
            if (GodotObject.IsInstanceValid(claw)) claw.ClawFrameChanged -= trace.OnClawFrameChanged;
            ReleaseAll();
            fixture.QueueFree();
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            await PhysicsFrames(tree, 2);
        }
    }

    private static async Task AssertEveryCatchLayerReturns(SceneTree tree)
    {
        CatchReturnCase[] cases =
        [
            new(LegacyTeachingSectors[1], "descent_grip_lesson", "GripStoneCatchZone",
                "Gameplay/GrappleSurfaces/DedicatedLessonAnchor", new Vector2(470, 330), false),
            new(LegacyTeachingSectors[3], "ascent_grapple_one", "CatchOne",
                "Gameplay/Collision/CatchReturnEdgeOne/GrappleSurfaceProfile",
                new Vector2(822, 1070), true),
            new(LegacyTeachingSectors[3], "ascent_grapple_two", "CatchTwo",
                "Gameplay/Collision/CatchReturnWallTwo/GrappleSurfaceProfile",
                new Vector2(1040, 790), true),
            new(LegacyTeachingSectors[3], "ascent_grapple_three", "CatchThree",
                "Gameplay/Collision/CatchReturnCeilingThree/GrappleSurfaceProfile",
                new Vector2(890, 618), true)
        ];
        foreach (CatchReturnCase item in cases)
            await AssertCatchLayerReturns(tree, item);
    }

    private static async Task AssertAscentAnchorSightlineMatrix(SceneTree tree)
    {
        string[] segmentIds =
        [
            "ascent_grapple_one",
            "ascent_grapple_two",
            "ascent_grapple_three"
        ];
        var failures = new List<string>();
        foreach (string segmentId in segmentIds)
        {
            foreach (float fraction in new[] { .2f, .5f, .8f })
            {
                ReleaseAll();
                using var owned = new LegacyTeachingSectorFixture(LegacyTeachingSectors[3],
                    $"Task3AscentSightline_{segmentId}_{fraction:0.0}");
                var fixture = owned.Root;
                PrologueSector sector = owned.Sector;
                TraversalSegment2D segment = sector.GetNodeOrNull<TraversalSegment2D>(
                    $"Gameplay/TraversalContracts/{segmentId}");
                TestAssert.True(segment != null && segment.SegmentId.ToString() == segmentId &&
                    segment.Kind == TraversalSegmentKind.Assisted,
                    "owned legacy sightline admits the exact assisted teaching segment before mounting");
                GrappleSurface2D target = sector.GetNodeOrNull<GrappleSurface2D>(segment.AssistTargetPath);
                TestAssert.True(target != null, "owned legacy sightline admits its exact saved assist target");
                CharacterBody2D player = owned.CreatePlayer(new Vector2(
                    segment.LaunchZone.Position.X + segment.LaunchZone.Size.X * fraction,
                    segment.LaunchZone.GetCenter().Y));
                tree.Root.AddChild(fixture);
                try
                {
                    await WaitForFloor(tree, player);
                    var resolver = new MechanicalClawTargetResolver();
                    MechanicalClawTargetResolution resolution = resolver.Resolve(
                        player, target.WorldAnchor);
                    bool insideLaunch = segment.LaunchZone.HasPoint(player.Position);
                    bool exact = resolution.Surface == target;
                    bool visible = resolution.Snapshot.HasLineOfSight;
                    string firstHit = FirstRayHit(player, target.WorldAnchor);
                    GD.Print($"TASK3_ASCENT_SIGHTLINE_CELL segment={segmentId} " +
                        $"sample={fraction:0.0} player={player.Position} target={target.WorldAnchor} " +
                        $"inside_launch={insideLaunch} exact={exact} visible={visible} " +
                        $"first_hit={firstHit}");
                    if (!insideLaunch || !exact || !visible)
                        failures.Add($"{segmentId}/{fraction:0.0}: player={player.Position} " +
                            $"inside={insideLaunch} exact={exact} visible={visible} hit={firstHit}");
                    owned.AssertNoRecovery();
                }
                finally
                {
                    ReleaseAll();
                    fixture.QueueFree();
                    await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
                    await PhysicsFrames(tree, 2);
                }
            }
        }
        TestAssert.Equal(0, failures.Count,
            $"all three ascent anchors remain clear from all three real launch samples: " +
            string.Join(" | ", failures));
        GD.Print("TASK3_ASCENT_SIGHTLINE_MATRIX_OK segments=3 samples_each=3 exact_targets=True");
    }

    // Break caught: a catch shelf that catches the player but offers no real-input path back to the
    // matching grapple landing converts a recoverable miss into a soft lock.
    private static async Task AssertCatchLayerReturns(SceneTree tree, CatchReturnCase expected)
    {
        ReleaseAll();
        using var owned = new LegacyTeachingSectorFixture(expected.Sector, $"Task3CatchReturn{expected.CatchName}");
        var fixture = owned.Root;
        PrologueSector sector = owned.Sector;
        TraversalSegment2D segment = sector.GetNodeOrNull<TraversalSegment2D>(
            $"Gameplay/TraversalContracts/{expected.SegmentId}");
        StaticBody2D catchLayer = sector.GetNodeOrNull<StaticBody2D>(
            $"Gameplay/Collision/{expected.CatchName}");
        GrappleSurface2D retryTarget = sector.GetNodeOrNull<GrappleSurface2D>(expected.TargetPath);
        TestAssert.True(segment != null && segment.SegmentId.ToString() == expected.SegmentId &&
            catchLayer != null && retryTarget != null,
            "owned legacy return admits exact segment/catch/return profile before mounting");
        RectangleShape2D shape = catchLayer.GetNode<CollisionShape2D>("Collision").Shape as
            RectangleShape2D ?? throw new InvalidOperationException("catch layer must be rectangular");
        CharacterBody2D player = owned.CreatePlayer(new Vector2(catchLayer.Position.X,
            catchLayer.Position.Y - shape.Size.Y / 2f - PlayerCollisionHalfHeight));
        tree.Root.AddChild(fixture);
        MechanicalClawController claw = player.GetNode<MechanicalClawController>("MechanicalClaw");
        var trace = new RouteTrace(player.GetNode<PlayerMotor>("Motor"), claw)
        {
            ExpectedTarget = retryTarget.GetInstanceId()
        };
        KillZone[] killZones = sector.GetNode("Gameplay/FallRecovery").GetChildren()
            .OfType<KillZone>().ToArray();
        int killZoneEntries = 0;
        foreach (KillZone killZone in killZones) killZone.BodyEntered += OnKillZoneEntered;
        claw.ClawFrameChanged += trace.OnClawFrameChanged;
        try
        {
            await WaitForFloor(tree, player);
            TestAssert.True(IsStandingOn(player, catchLayer),
                $"{expected.CatchName} retry begins on its exact physical catch layer");
            TestAssert.True(retryTarget.UseCollisionHitPoint == expected.TerrainReturn,
                $"{expected.CatchName} retry target uses the authored point/terrain profile kind");
            Vector2 retryAim = sector.ToGlobal(expected.AimLocal);
            var resolver = new MechanicalClawTargetResolver();
            MechanicalClawTargetResolution preflight = resolver.Resolve(player, retryAim);
            TestAssert.True(preflight.Surface == retryTarget && preflight.Snapshot.HasLineOfSight,
                $"{expected.CatchName} retry aim resolves its exact typed return surface; " +
                $"aim={retryAim} first_hit={FirstRayHit(player, retryAim)}");
            if (expected.CatchName == "CatchThree")
                AssertCatchThreeRayNotch(sector, player, segment, retryAim, retryTarget, preflight);
            if (expected.TerrainReturn)
            {
                float hitDistance = player.GlobalPosition.DistanceTo(preflight.Snapshot.WorldAnchor);
                TestAssert.True(hitDistance >= 360f && hitDistance <= 520f,
                    $"{expected.CatchName} typed return face stays inside [360,520]; " +
                    $"actual={hitDistance:0.00} hit={preflight.Snapshot.WorldAnchor}");
            }
            GD.Print($"TASK3_CATCH_RETURN_DIRECT_RAY catch={expected.CatchName} " +
                $"segment={expected.SegmentId} from={player.GlobalPosition} " +
                $"aim={retryAim} hit={preflight.Snapshot.WorldAnchor} " +
                $"first_hit={FirstRayHit(player, retryAim)}");
            await DriveAssisted(tree, player, sector, segment, trace,
                retryTarget, boostImmediately: expected.TerrainReturn, aimOverride: retryAim,
                useCurrentPositionForDirection: expected.TerrainReturn);
            AssertLanding(player, segment, trace);
            if (expected.TerrainReturn)
                TestAssert.True(trace.LatchedAnchor.DistanceTo(retryTarget.WorldAnchor) > 8f,
                    $"{expected.CatchName} retry latches the real collision face rather than " +
                    $"the return body's decorative center; center={retryTarget.WorldAnchor} " +
                    $"hit={trace.LatchedAnchor}");
            TestAssert.Equal(0, trace.Motor.AcceptedDashCount,
                $"{expected.CatchName} return uses no Dash");
            TestAssert.Equal(0, killZoneEntries,
                $"{expected.CatchName} return enters zero KillZone recovery volumes");
            owned.AssertNoRecovery();
            GD.Print($"TASK3_CATCH_RETURN_OK catch={expected.CatchName} " +
                $"segment={expected.SegmentId} position={player.Position} stable=12 margin=15% " +
                "killzone_entries=0");
        }
        finally
        {
            if (GodotObject.IsInstanceValid(claw)) claw.ClawFrameChanged -= trace.OnClawFrameChanged;
            foreach (KillZone killZone in killZones)
                if (GodotObject.IsInstanceValid(killZone)) killZone.BodyEntered -= OnKillZoneEntered;
            ReleaseAll();
            fixture.QueueFree();
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            await PhysicsFrames(tree, 2);
        }

        void OnKillZoneEntered(Node2D entered)
        {
            if (entered == player) killZoneEntries++;
        }
    }

    private static void AssertCatchThreeRayNotch(PrologueSector sector, CharacterBody2D player,
        TraversalSegment2D segment, Vector2 retryAim, GrappleSurface2D retryTarget,
        MechanicalClawTargetResolution preflight)
    {
        const float MinimumClearance = 12f;
        StaticBody2D ceiling = retryTarget.GetParent() as StaticBody2D ??
            throw new InvalidOperationException("CatchThree return must be typed terrain");
        RectangleShape2D ceilingShape = ceiling.GetNode<CollisionShape2D>("Collision").Shape as
            RectangleShape2D ?? throw new InvalidOperationException("CatchThree ceiling must be rectangular");
        StaticBody2D landing = sector.GetNode<StaticBody2D>("Gameplay/Collision/LandingThree");
        RectangleShape2D landingShape = landing.GetNode<CollisionShape2D>("Collision").Shape as
            RectangleShape2D ?? throw new InvalidOperationException("LandingThree must be rectangular");
        GrappleSurface2D anchorTwo = sector.GetNode<GrappleSurface2D>(
            "Gameplay/GrappleSurfaces/AscentAnchorTwo");
        CircleShape2D anchorTwoShape = anchorTwo.GetNode<Area2D>("TargetCollision")
            .GetNode<CollisionShape2D>("Collision").Shape as CircleShape2D ??
            throw new InvalidOperationException("AnchorTwo must retain its point-target circle");
        GrappleSurface2D anchorThree = sector.GetNode<GrappleSurface2D>(
            "Gameplay/GrappleSurfaces/AscentAnchorThree");
        Area2D anchorThreeCollision = anchorThree.GetNode<Area2D>("TargetCollision");

        Vector2 ceilingHit = preflight.Snapshot.WorldAnchor;
        float hitDistance = player.GlobalPosition.DistanceTo(ceilingHit);
        TestAssert.Near(882f, ceilingHit.X, 3f,
            "CatchThree retry hits the authored ceiling underside near x=882 rather than an anchor");
        TestAssert.Near(630f, ceilingHit.Y, 2f,
            "CatchThree retry hits the authored ceiling underside at y=630");
        TestAssert.True(hitDistance >= 360f && hitDistance <= 520f,
            $"CatchThree ceiling hit stays inside [360,520]; actual={hitDistance:0.00}");

        float anchorTwoClearance = DistanceToSegment(anchorTwo.GlobalPosition,
            player.GlobalPosition, retryAim) - anchorTwoShape.Radius;
        float landingTop = landing.GlobalPosition.Y - landingShape.Size.Y / 2f;
        float landingRayX = RayXAtY(player.GlobalPosition, retryAim, landingTop);
        float landingStart = landing.GlobalPosition.X - landingShape.Size.X / 2f;
        Vector2 anchorThreeLaunch = new(
            segment.LaunchZone.Position.X + segment.LaunchZone.Size.X * .8f,
            segment.LaunchZone.GetCenter().Y);
        RayHit? anchorThreeHit = FirstRayHitDetail(player, anchorThreeLaunch,
            anchorThree.GlobalPosition);
        float ceilingLeft = ceiling.GlobalPosition.X - ceilingShape.Size.X / 2f;
        float ceilingUnderside = ceiling.GlobalPosition.Y + ceilingShape.Size.Y / 2f;
        float verticalHeadroom = landingTop - ceilingUnderside - PlayerCollisionHalfHeight * 2f;

        TestAssert.True(anchorTwoClearance >= MinimumClearance,
            $"CatchThree return line clears AnchorTwo's target circle by at least {MinimumClearance:0}px; " +
            $"actual={anchorTwoClearance:0.00}");
        TestAssert.True(landingStart - landingRayX >= MinimumClearance,
            $"CatchThree return line crosses LandingThree's height through a {MinimumClearance:0}px-plus " +
            $"notch; ray_x={landingRayX:0.00} landing_start={landingStart:0.00}");
        TestAssert.True(verticalHeadroom >= MinimumClearance,
            $"CatchThree ceiling and landing retain at least {MinimumClearance:0}px of physical " +
            $"headroom for the player; actual={verticalHeadroom:0.00}");
        TestAssert.True(anchorThreeHit.HasValue && anchorThreeHit.Value.Collider == anchorThreeCollision,
            "all three main-anchor sightline samples retain the physical AnchorThree target before the ceiling");
        TestAssert.True(ceilingLeft - anchorThreeHit.Value.Position.X >= 32f,
            $"AnchorThree's rightmost representative ray stays structurally clear of the ceiling; " +
            $"hit_x={anchorThreeHit.Value.Position.X:0.00} ceiling_left={ceilingLeft:0.00}");
        GD.Print($"TASK3_CATCH_THREE_RAY_NOTCH_OK ceiling_hit={ceilingHit} " +
            $"distance={hitDistance:0.00} anchor_two_clearance={anchorTwoClearance:0.00} " +
            $"landing_notch={landingStart - landingRayX:0.00} " +
            $"anchor_three_gap={ceilingLeft - anchorThreeHit.Value.Position.X:0.00} " +
            $"vertical_headroom={verticalHeadroom:0.00}");
    }

    private static bool HasLandingMargin(Rect2 zone, Vector2 point)
    {
        Vector2 inset = zone.Size * LandingMarginRatio;
        return new Rect2(zone.Position + inset, zone.Size - inset * 2f).HasPoint(point);
    }

    private static float RequiredRopeForLanding(Vector2 anchor, Rect2 landingZone)
    {
        float safeStart = landingZone.Position.X + landingZone.Size.X * LandingMarginRatio;
        float safeEnd = landingZone.End.X - landingZone.Size.X * LandingMarginRatio;
        float nearestSafeX = Mathf.Clamp(anchor.X, safeStart, safeEnd);
        return anchor.DistanceTo(new Vector2(nearestSafeX, landingZone.GetCenter().Y));
    }

    private static int CatchDirectionPhase()
    {
        const string prefix = "--task3-catch-phase=";
        string argument = OS.GetCmdlineUserArgs().SingleOrDefault(item =>
            item.StartsWith(prefix, StringComparison.Ordinal));
        if (argument == null) return 6;
        int phase = int.Parse(argument.Substring(prefix.Length));
        if (phase is not (0 or 4 or 6 or 8 or 12))
            throw new InvalidOperationException($"unsupported CatchOne phase {phase}");
        return phase;
    }

    private static bool IsAttached(MechanicalClawState state) => state is
        MechanicalClawState.Latched or MechanicalClawState.Reel or MechanicalClawState.Swing or
        MechanicalClawState.Boost;

    private static string FirstRayHit(CharacterBody2D player, Vector2 target)
    {
        RayHit? hit = FirstRayHitDetail(player, player.GlobalPosition, target);
        return hit.HasValue ? $"{hit.Value.Collider.GetPath()}@{hit.Value.Position}" : "None";
    }

    private static RayHit? FirstRayHitDetail(CharacterBody2D player, Vector2 from, Vector2 target)
    {
        PhysicsRayQueryParameters2D query = PhysicsRayQueryParameters2D.Create(
            from, target);
        query.CollideWithAreas = true;
        query.CollideWithBodies = true;
        query.CollisionMask = CombatCollisionLayers.World | CombatCollisionLayers.Enemy |
            CombatCollisionLayers.ThreadTarget;
        var excluded = new Godot.Collections.Array<Rid>();
        CollectCollisionRids(player, excluded);
        query.Exclude = excluded;
        Godot.Collections.Dictionary hit = player.GetWorld2D().DirectSpaceState.IntersectRay(query);
        if (hit.Count == 0) return null;
        Node collider = hit.ContainsKey("collider") ? hit["collider"].AsGodotObject() as Node : null;
        Vector2 position = hit.ContainsKey("position") ? hit["position"].AsVector2() : Vector2.Zero;
        return collider == null ? null : new RayHit(collider, position);
    }

    private static float DistanceToSegment(Vector2 point, Vector2 start, Vector2 end)
    {
        Vector2 span = end - start;
        float lengthSquared = span.LengthSquared();
        if (lengthSquared <= 0f) return point.DistanceTo(start);
        float fraction = Mathf.Clamp((point - start).Dot(span) / lengthSquared, 0f, 1f);
        return point.DistanceTo(start + span * fraction);
    }

    private static float RayXAtY(Vector2 start, Vector2 end, float y)
    {
        float vertical = end.Y - start.Y;
        if (Mathf.IsZeroApprox(vertical))
            throw new InvalidOperationException("ray-notch diagnostic requires a non-horizontal ray");
        return start.X + (y - start.Y) * (end.X - start.X) / vertical;
    }

    private static string FirstSlideHit(CharacterBody2D player)
    {
        if (player.GetSlideCollisionCount() == 0) return "None";
        KinematicCollision2D collision = player.GetSlideCollision(0);
        Node collider = collision.GetCollider() as Node;
        return $"{collider?.GetPath().ToString() ?? "Unknown"}@{collision.GetPosition()}";
    }

    private static void CollectCollisionRids(Node node, Godot.Collections.Array<Rid> rids)
    {
        if (node is CollisionObject2D collisionObject) rids.Add(collisionObject.GetRid());
        foreach (Node child in node.GetChildren()) CollectCollisionRids(child, rids);
    }

    private static async Task AssertOrdinaryJumpCannotReplaceClaw(SceneTree tree,
        SectorCase sectorCase, string segmentId)
    {
        ReleaseAll();
        using var owned = new LegacyTeachingSectorFixture(sectorCase, $"Task8JumpFallback_{segmentId}");
        var fixture = owned.Root;
        PrologueSector sector = owned.Sector;
        TraversalSegment2D segment = sector.GetNodeOrNull<TraversalSegment2D>(
            $"Gameplay/TraversalContracts/{segmentId}");
        TestAssert.True(segment != null && segment.SegmentId.ToString() == segmentId &&
            segment.Kind == TraversalSegmentKind.Assisted,
            "owned legacy ordinary-jump negative admits its exact assisted segment");
        CharacterBody2D player = owned.CreatePlayer(segment.LaunchZone.GetCenter());
        tree.Root.AddChild(fixture);
        var trace = new RouteTrace(player.GetNode<PlayerMotor>("Motor"),
            player.GetNode<MechanicalClawController>("MechanicalClaw"));
        try
        {
            await WaitForFloor(tree, player);
            await DriveSingleJump(tree, player, segment, trace, probeSecondJump: true);
            await PhysicsFrames(tree, 24);
            TestAssert.True(!trace.SecondAirJumpAccepted,
                $"{segmentId} rejects a second Space press during its ordinary-jump probe");
            TestAssert.True(!segment.LandingZone.HasPoint(player.Position),
                $"{segmentId} ordinary jump is intentionally insufficient; " +
                $"position={player.Position} landing={segment.LandingZone}");
            TestAssert.True(player.IsOnFloor() &&
                sector.GetNode("Gameplay/Collision").GetChildren().Any(node =>
                    node.HasMeta("catch_shelf") && IsStandingOn(player, (StaticBody2D)node)),
                $"{segmentId} failed ordinary jump settles on a safe catch shelf; " +
                $"position={player.Position} on_floor={player.IsOnFloor()} frames={trace.Frames}");
            owned.AssertNoRecovery();
        }
        finally
        {
            ReleaseAll();
            fixture.QueueFree();
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            await PhysicsFrames(tree, 2);
        }
    }

    public static async Task RunShieldLaneOnly(SceneTree tree)
    {
        ActiveTree = tree;
        try { await AssertShieldLaneBehavior(tree); }
        finally { ActiveInputCanvas = null; }
    }

    private static async Task AssertShieldLaneBehavior(SceneTree tree)
    {
        AssertShieldLaneAdmissionNegatives();
        await AssertShieldLaneFailureOwnership(tree);
        await AssertShieldLaneUnguarded(tree);
        await AssertShieldLaneGuarded(tree);
        GD.Print("SHIELD_LANE_KEYBOARD_OK cases=2 admission_negatives=3 supported_exit=true physical_keys=true");
    }

    private static Task AssertShieldLaneUnguarded(SceneTree tree) => RunSavedShieldLaneCase(tree, false);
    private static Task AssertShieldLaneGuarded(SceneTree tree) => RunSavedShieldLaneCase(tree, true);

    private sealed record ShieldLaneFixture(RangedFairnessRecoveryProbe Root, PrologueSector Sector,
        CharacterBody2D Player, CanyonShieldLaneHazard Lane, StaticBody2D Entry, StaticBody2D Exit);

    private static IEnumerable<Node> ShieldLaneNodes(Node root)
    {
        foreach (Node child in root.GetChildren())
        {
            yield return child;
            foreach (Node descendant in ShieldLaneNodes(child)) yield return descendant;
        }
    }

    private static Transform2D ShieldLanePose(Node node)
    {
        Transform2D pose = Transform2D.Identity;
        for (Node current = node; current != null; current = current.GetParent())
            if (current is Node2D item) pose = item.Transform * pose;
        return pose;
    }

    private static Rect2 ShieldLaneRectangle(Node2D body)
    {
        var collision = body.GetNodeOrNull<CollisionShape2D>("Collision");
        TestAssert.True(collision is { Disabled: false, OneWayCollision: false, Shape: RectangleShape2D },
            $"shield lane fixture enabled rectangular support {body.Name}");
        var shape = (RectangleShape2D)collision.Shape;
        Transform2D pose = ShieldLanePose(collision);
        TestAssert.True(pose.X.IsEqualApprox(Vector2.Right) && pose.Y.IsEqualApprox(Vector2.Down) &&
            float.IsFinite(shape.Size.X) && float.IsFinite(shape.Size.Y) && shape.Size.X > 0 && shape.Size.Y > 0,
            $"shield lane fixture finite translation-only rectangle {body.Name}");
        return new Rect2(pose.Origin - shape.Size * .5f, shape.Size);
    }

    private static (Vector2 Center, float Radius, float HalfHeight) ShieldLaneCapsule(CharacterBody2D player)
    {
        var collider = player.GetNode<CollisionShape2D>("BodyCollider");
        TestAssert.True(collider is { Disabled: false, Shape: CapsuleShape2D },
            "shield lane fixture real enabled capsule");
        var capsule = (CapsuleShape2D)collider.Shape;
        Transform2D pose = ShieldLanePose(collider);
        TestAssert.True(pose.X.IsEqualApprox(Vector2.Right) && pose.Y.IsEqualApprox(Vector2.Down) &&
            float.IsFinite(capsule.Radius) && float.IsFinite(capsule.Height) &&
            capsule.Radius > 0 && capsule.Height >= 2 * capsule.Radius,
            "shield lane fixture finite unscaled capsule");
        return (pose.Origin, capsule.Radius, capsule.Height * .5f);
    }

    private static ShieldLaneFixture CreateShieldLaneFixture()
    {
        var root = new RangedFairnessRecoveryProbe { Name = "SavedV2ShieldLane" };
        try
        {
            var sector = GD.Load<PackedScene>(Sectors[2].Path).Instantiate<PrologueSector>();
            root.AddChild(sector);
            CharacterBody2D player = NewPlayer();
            root.AddChild(player);
            var lane = sector.GetNode<CanyonShieldLaneHazard>("Gameplay/ShieldLane");
            var entry = sector.GetNode<StaticBody2D>("Gameplay/Collision/GearEntry");
            var exit = sector.GetNode<StaticBody2D>("Gameplay/Collision/GearPistonOne");
            var fixture = new ShieldLaneFixture(root, sector, player, lane, entry, exit);
            Rect2 floor = ShieldLaneRectangle(entry), field = ShieldLaneRectangle(lane);
            var capsule = ShieldLaneCapsule(player);
            float left = floor.Position.X + capsule.Radius + 2f;
            float right = field.Position.X - capsule.Radius - 2f;
            TestAssert.True(right >= left, "shield lane fixture has a full-capsule pre-field support window");
            player.Position = new Vector2((left + right) * .5f, floor.Position.Y - capsule.HalfHeight);
            // Only this owned local fixture is rebound. The saved six-parent production path is not edited.
            foreach (KillZone zone in ShieldLaneNodes(sector).OfType<KillZone>())
                zone.RoomControllerPath = zone.GetPathTo(root);
            AssertShieldLaneFixtureAdmission(fixture);
            return fixture;
        }
        catch { root.Free(); throw; }
    }

    private static void AssertShieldLaneFixtureAdmission(ShieldLaneFixture fixture)
    {
        TestAssert.Equal(Vector2.Zero, fixture.Sector.Position, "shield lane fixture local-origin sector");
        TestAssert.Equal(new Rect2(800, 396, 300, 40), ShieldLaneRectangle(fixture.Entry),
            "shield lane fixture independently pinned entry support");
        TestAssert.Equal(new Rect2(1160, 316, 212, 80), ShieldLaneRectangle(fixture.Exit),
            "shield lane fixture independently pinned exit support");
        Rect2 field = ShieldLaneRectangle(fixture.Lane);
        TestAssert.Equal(new Rect2(850, 270, 260, 120), field, "shield lane fixture actual full danger rectangle");
        var capsule = ShieldLaneCapsule(fixture.Player);
        Rect2 floor = ShieldLaneRectangle(fixture.Entry);
        TestAssert.True(capsule.Center.X - capsule.Radius >= floor.Position.X + 2f &&
            capsule.Center.X + capsule.Radius <= floor.End.X - 2f &&
            MathF.Abs(capsule.Center.Y + capsule.HalfHeight - floor.Position.Y) <= .01f &&
            capsule.Center.X + capsule.Radius <= field.Position.X - 2f,
            "shield lane fixture initial complete capsule support before real field");
        KillZone[] zones = ShieldLaneNodes(fixture.Sector).OfType<KillZone>().ToArray();
        TestAssert.True(zones.Length > 0 && zones.All(zone =>
            zone.GetNodeOrNull<Node>(zone.RoomControllerPath) == fixture.Root),
            "shield lane fixture typed recovery identity");
        TestAssert.True(fixture.Root is ILevelRecovery && fixture.Root.RecoveryRequests == 0,
            "shield lane fixture recovery observer cannot hide an unsafe route");
    }

    private static void AssertShieldLaneAdmissionNegatives()
    {
        void Reject(string reason, Action<ShieldLaneFixture> mutate)
        {
            ShieldLaneFixture fixture = CreateShieldLaneFixture();
            try
            {
                mutate(fixture);
                bool rejected = false;
                try { AssertShieldLaneFixtureAdmission(fixture); }
                catch (InvalidOperationException exception) when (
                    exception.Message.StartsWith("Assertion failed: shield lane fixture", StringComparison.Ordinal) &&
                    exception.Message.Contains(reason, StringComparison.Ordinal)) { rejected = true; }
                TestAssert.True(rejected, "shield lane admission rejects its intended corrupted fixture: " + reason);
            }
            finally { fixture.Root.Free(); }
        }
        Reject("initial complete capsule support", fixture => fixture.Player.Position = new Vector2(300, 574));
        Reject("enabled rectangular support GearEntry", fixture => fixture.Entry.GetNode<CollisionShape2D>("Collision").Disabled = true);
        Reject("typed recovery identity", fixture =>
            ShieldLaneNodes(fixture.Sector).OfType<KillZone>().First().RoomControllerPath = new NodePath(".."));
        GD.Print("SHIELD_LANE_ADMISSION_NEGATIVES_OK cases=3 same_checker=true");
    }

    private static bool ShieldLaneSupported(CharacterBody2D player, StaticBody2D support)
    {
        var capsule = ShieldLaneCapsule(player);
        Rect2 floor = ShieldLaneRectangle(support);
        return Task4HasFloorContact(player, support) &&
            MathF.Abs(capsule.Center.Y + capsule.HalfHeight - floor.Position.Y) <= 1f &&
            capsule.Center.X - capsule.Radius >= floor.Position.X + 2f &&
            capsule.Center.X + capsule.Radius <= floor.End.X - 2f;
    }

    private static async Task AssertShieldLaneFailureOwnership(SceneTree tree)
    {
        const string cleanupFault = "shield_lane_owned_cleanup_probe";
        var failures = new List<string>();
        foreach (bool preparationFault in new[] { true, false })
        {
            ShieldLaneFixture captured = null;
            int rootChildren = tree.Root.GetChildCount();
            bool rejected = false, rootReleased = false, driverReleased = false, keysReleased = false;
            try
            {
                await RunSavedShieldLaneCase(tree, false, fixture =>
                {
                    captured = fixture;
                    if (!preparationFault) return;
                    // Wrong type, not a missing path: provoke the real typed lookup without native error noise.
                    Node health = fixture.Player.GetNode("Health");
                    fixture.Player.RemoveChild(health);
                    health.Free();
                    fixture.Player.AddChild(new Node2D { Name = "Health" });
                }, preparationFault ? null : () => throw new InvalidOperationException(cleanupFault));
            }
            catch (InvalidCastException error) when (preparationFault &&
                error.Message.Contains(typeof(HealthComponent).FullName, StringComparison.Ordinal))
            { rejected = true; }
            catch (InvalidOperationException error) when (!preparationFault && error.Message == cleanupFault)
            { rejected = true; }
            finally
            {
                rootReleased = captured != null && !GodotObject.IsInstanceValid(captured.Root);
                driverReleased = KeyboardRouteInputDriver.Current == null;
                keysReleased = !new[] { Key.A, Key.D, Key.W, Key.S, Key.Space, Key.Shift, Key.J, Key.K, Key.L, Key.Q }
                    .Any(Input.IsPhysicalKeyPressed);
                // A failing ownership control must report the leak, then clean its own surviving objects.
                // This fallback is never counted as the case releasing its ownership.
                try { KeyboardRouteInputDriver.Current?.Dispose(); }
                finally
                {
                    if (captured != null && GodotObject.IsInstanceValid(captured.Root))
                    {
                        if (captured.Root.IsInsideTree())
                        {
                            captured.Root.QueueFree();
                            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
                            await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
                            await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
                        }
                        else captured.Root.Free();
                    }
                }
            }
            bool unchangedChildren = tree.Root.GetChildCount() == rootChildren;
            GD.Print($"SHIELD_LANE_FAILURE_OWNERSHIP_CONTROL stage={(preparationFault ? "typed_lookup" : "cleanup")} " +
                $"intended_rejection={rejected} root_released_before_fallback={rootReleased} " +
                $"driver_released_before_fallback={driverReleased} keys_released_before_fallback={keysReleased} " +
                $"root_children_unchanged={unchangedChildren}");
            if (!rejected || !rootReleased || !driverReleased || !keysReleased || !unchangedChildren)
                failures.Add(preparationFault ? "typed_lookup" : "cleanup");
        }
        TestAssert.True(failures.Count == 0,
            "shield lane failure ownership releases root, driver and keys before control fallback: " + string.Join(",", failures));
        GD.Print("SHIELD_LANE_FAILURE_OWNERSHIP_OK cases=2 fallback_not_counted=true");
    }

    private static async Task RunSavedShieldLaneCase(SceneTree tree, bool guarded,
        Action<ShieldLaneFixture> prepare = null, Action beforeDriverCleanup = null)
    {
        foreach (Key key in new[] { Key.A, Key.D, Key.W, Key.S, Key.Space, Key.Shift, Key.J, Key.K, Key.L, Key.Q })
            KeyboardActionRuntimeTests.KeyEvent(key, false);
        ShieldLaneFixture fixture = CreateShieldLaneFixture();
        // Own the tree before any typed component lookup, subscription or driver setup can fail.
        try { await RunPreparedShieldLaneCase(tree, guarded, fixture, prepare, beforeDriverCleanup); }
        finally
        {
            if (fixture.Root.IsInsideTree())
            {
                fixture.Root.QueueFree();
                await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
                await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
                await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
            }
            else fixture.Root.Free();
        }
    }

    private static async Task RunPreparedShieldLaneCase(SceneTree tree, bool guarded,
        ShieldLaneFixture fixture, Action<ShieldLaneFixture> prepare, Action beforeDriverCleanup)
    {
        prepare?.Invoke(fixture);
        CharacterBody2D player = fixture.Player;
        var input = player.GetNode<PlayerInputReader>("Input");
        var health = player.GetNode<HealthComponent>("Health");
        var motor = player.GetNode<PlayerMotor>("Motor");
        var energy = player.GetNode<PlayerEnergyController>("Energy");
        var shield = player.GetNode<PlayerShieldController>("Shield");
        var claw = player.GetNode<MechanicalClawController>("MechanicalClaw");
        Node actors = fixture.Sector.GetNode("Gameplay/Actors");
        CanyonEncounterGroup[] groups = ShieldLaneNodes(fixture.Sector).OfType<CanyonEncounterGroup>().ToArray();
        KeyboardRouteInputDriver driver = null;
        int damageEvents = 0, healEvents = 0, deaths = 0, guardEvents = 0;
        int entryStable = 0, exitStable = 0, overlapSamples = 0, spaceEdges = 0, lDown = 0, lUp = 0;
        ulong lastSample = 0, playerId = player.GetInstanceId(), laneId = fixture.Lane.GetInstanceId();
        bool lastL = false, guardSeen = false, invalidGuard = false, overlapGuardLost = false;
        Exception observationFailure = null;
        HealthComponent.DamagedEventHandler damaged = (_, _) => damageEvents++;
        HealthComponent.HealedEventHandler healed = (_, _) => healEvents++;
        HealthComponent.DiedEventHandler died = () => deaths++;
        void Resolved(PlayerShieldResult result, ulong transition)
        {
            guardEvents++;
            // The callback precedes the lane's PulseCount increment. Correlate totals after the completed tick.
            invalidGuard |= !result.Guarded || result.DamageAmount != 0 || result.KnockbackScale != 0 ||
                result.SpendHeavyEnergy || result.BrokeGuard || !fixture.Lane.GetOverlappingBodies().Contains(player) ||
                !input.Current.KeyboardShieldHeld || !Input.IsPhysicalKeyPressed(Key.L) || !shield.IsActive;
        }
        void Observe()
        {
            try
            {
                if (driver == null || input.CurrentFrameSequence == lastSample) return;
                lastSample = input.CurrentFrameSequence;
                driver.ObserveFrame();
                if (player.GetInstanceId() != playerId || fixture.Lane.GetInstanceId() != laneId ||
                    fixture.Root.RecoveryRequests != 0 || deaths != 0 || healEvents != 0 ||
                    actors.GetChildCount() != 0 || groups.Any(group => group.IsActive || group.LiveActorCount != 0))
                    throw new InvalidOperationException("shield lane observations retain one living player, no rescue/heal/other actors");
                if (input.Current.AttackPressed || input.Current.ClawPressed || input.Current.DashPressed ||
                    Input.IsPhysicalKeyPressed(Key.J) || Input.IsPhysicalKeyPressed(Key.K) || Input.IsPhysicalKeyPressed(Key.Shift))
                    throw new InvalidOperationException("shield lane crossing cannot borrow attack/claw/dash inputs");
                if (input.Current.JumpPressed) spaceEdges++;
                bool currentL = input.Current.KeyboardShieldHeld;
                if (currentL && !lastL) lDown++;
                if (!currentL && lastL) lUp++;
                lastL = currentL;
                guardSeen |= shield.IsActive;
                entryStable = ShieldLaneSupported(player, fixture.Entry) ? entryStable + 1 : 0;
                var capsule = ShieldLaneCapsule(player);
                bool outside = capsule.Center.X - capsule.Radius > ShieldLaneRectangle(fixture.Lane).End.X + 2f &&
                    !fixture.Lane.GetOverlappingBodies().Contains(player);
                exitStable = outside && ShieldLaneSupported(player, fixture.Exit) ? exitStable + 1 : 0;
                if (fixture.Lane.GetOverlappingBodies().Contains(player))
                {
                    overlapSamples++;
                    if (guarded && (!currentL || !Input.IsPhysicalKeyPressed(Key.L) || !shield.IsActive ||
                        shield.GuardDirection.Dot(Vector2.Right) < .99f)) overlapGuardLost = true;
                }
            }
            catch (Exception error) { observationFailure ??= error; }
        }
        async Task Tick()
        {
            await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
            if (observationFailure != null) throw new InvalidOperationException("shield lane completed-physics observation failed", observationFailure);
            TestAssert.True(fixture.Root.RecoveryRequests == 0 && deaths == 0 && healEvents == 0,
                "shield lane does not recover, die or heal while waiting");
        }
        bool subscribed = false;
        try
        {
            tree.Root.AddChild(fixture.Root);
            driver = KeyboardRouteInputDriver.Begin(player);
            health.Damaged += damaged; health.Healed += healed; health.Died += died;
            shield.ShieldResolved += Resolved;
            tree.PhysicsFrame += Observe;
            subscribed = true;
            for (int tick = 0; tick < 60 && entryStable < 3; tick++) await Tick();
            TestAssert.True(entryStable >= 3 && ShieldLaneSupported(player, fixture.Entry) &&
                !fixture.Lane.GetOverlappingBodies().Contains(player) && fixture.Lane.PulseCount == 0,
                $"shield lane has three fresh native full-capsule GearEntry contacts before danger; player={player.Position}");
            TestAssert.True(health.CurrentHealth == 5 && energy.EnergyRatio > .999f && energy.HeatRatio == 0 &&
                fixture.Lane.Damage == 1 && fixture.Lane.Knockback == 90 &&
                fixture.Lane.PulseIntervalSeconds == .28f && fixture.Lane.Monitoring && fixture.Lane.IsPhysicsProcessing(),
                "shield lane uses unchanged saved HP/energy/heat and running native pulse parameters");
            int pulsesBefore = fixture.Lane.PulseCount;
            int jumpsBefore = motor.AcceptedJumpCount, dashesBefore = motor.AcceptedDashCount;
            ulong castBefore = claw.CurrentFrame.CastId;
            ulong started = Engine.GetPhysicsFrames();
            int budget = guarded ? 150 : 120;
            if (guarded)
            {
                driver.Command(InputActions.KeyboardShield, true);
                for (int tick = 0; tick < 8 && !shield.IsActive && Engine.GetPhysicsFrames() - started < (ulong)budget; tick++) await Tick();
                TestAssert.True(shield.IsActive && input.Current.KeyboardShieldHeld && input.Current.ShieldHeld &&
                    Input.IsPhysicalKeyPressed(Key.L) && shield.GuardDirection.Dot(Vector2.Right) > .99f,
                    "real physical and sampled L sustains a right-facing guard before entry");
            }
            driver.Command(InputActions.MoveRight, true);
            if (!guarded)
            {
                while (damageEvents == 0 && Engine.GetPhysicsFrames() - started < (ulong)budget) await Tick();
                driver.Command(InputActions.MoveRight, false);
                TestAssert.True(damageEvents > 0 && health.CurrentHealth < 5 &&
                    fixture.Lane.PulseCount > pulsesBefore && overlapSamples > 0 && guardEvents == 0 &&
                    !shield.IsActive && lDown == 0 && driver.ShieldEdges == 0 &&
                    ShieldLaneCapsule(player).Center.X - ShieldLaneCapsule(player).Radius < ShieldLaneRectangle(fixture.Lane).End.X,
                    $"unguarded native lane pulse damages before whole-capsule exit; HP=5->{health.CurrentHealth} " +
                    $"pulses={fixture.Lane.PulseCount} player={player.Position} overlap_samples={overlapSamples}");
                TestAssert.True(spaceEdges == 0 && motor.AcceptedJumpCount == jumpsBefore &&
                    motor.AcceptedDashCount == dashesBefore && claw.CurrentFrame.CastId == castBefore,
                    "unguarded lane proof is D-only, with no hidden ability");
                GD.Print($"SHIELD_LANE_UNGUARDED_OK HP=5->{health.CurrentHealth} pulses={fixture.Lane.PulseCount} " +
                    $"overlap_samples={overlapSamples} actual_ticks={Engine.GetPhysicsFrames() - started}/120 " +
                    $"player={player.Position} damage_events={damageEvents} recovery=0 physical_D=true");
            }
            else
            {
                // Normal-world route's saved takeoff is2350 minus Basin's1280 offset.
                while (player.Position.X < 1070f && Engine.GetPhysicsFrames() - started < (ulong)budget) await Tick();
                TestAssert.True(ShieldLaneSupported(player, fixture.Entry) && player.Position.X >= 1070f,
                    "shield walk reaches the actual GearEntry takeoff under the unchanged deadline");
                ulong jumpIssued = Engine.GetPhysicsFrames();
                driver.Command(InputActions.Jump, true);
                bool spaceReleased = false, movementReleased = false;
                Rect2 landing = ShieldLaneRectangle(fixture.Exit);
                float stopX = landing.Position.X + ShieldLaneCapsule(player).Radius + 10f;
                while (exitStable < 3 && Engine.GetPhysicsFrames() - started < (ulong)budget)
                {
                    if (!spaceReleased && Engine.GetPhysicsFrames() - jumpIssued >= 18)
                    { driver.Command(InputActions.Jump, false); spaceReleased = true; }
                    float vx = MathF.Max(0, player.Velocity.X);
                    float stopping = vx * vx / (2f * motor.Tuning.GroundDeceleration) + vx / 60f;
                    if (!movementReleased && player.Position.X + stopping >= stopX)
                    { driver.Command(InputActions.MoveRight, false); movementReleased = true; }
                    await Tick();
                }
                driver.Command(InputActions.Jump, false);
                driver.Command(InputActions.MoveRight, false);
                float spent = energy.EnergyRatio, raised = energy.HeatRatio;
                TestAssert.True(exitStable >= 3 && ShieldLaneSupported(player, fixture.Exit) &&
                    !fixture.Lane.GetOverlappingBodies().Contains(player) && overlapSamples > 0 &&
                    fixture.Lane.PulseCount > pulsesBefore && guardEvents == fixture.Lane.PulseCount - pulsesBefore &&
                    !invalidGuard && !overlapGuardLost && guardSeen && shield.IsActive && !energy.IsOverheated &&
                    health.CurrentHealth == 5 && damageEvents == 0 && deaths == 0 && healEvents == 0 &&
                    driver.ShieldEdges == 1 && lDown == 1 && spaceEdges == 1 &&
                    motor.AcceptedJumpCount == jumpsBefore + 1 && motor.AcceptedDashCount == dashesBefore &&
                    claw.CurrentFrame.CastId == castBefore && driver.AttackEdges == 0 && driver.ClawEdges == 0 &&
                    spent < .999f && raised > 0,
                    $"guarded native whole-field jump finishes on exact PistonOne support; player={player.Position} " +
                    $"ticks={Engine.GetPhysicsFrames() - started}/150 stable={exitStable} " +
                    $"HP={health.CurrentHealth} damage={damageEvents} overlap={overlapSamples} lost_guard={overlapGuardLost} " +
                    $"pulses={fixture.Lane.PulseCount} guards={guardEvents}/{invalidGuard} L={lDown} " +
                    $"Space={spaceEdges}/{motor.AcceptedJumpCount - jumpsBefore} energy={spent} heat={raised}");
                GD.Print($"SHIELD_LANE_GUARDED_OK player={player.Position} actual_ticks={Engine.GetPhysicsFrames() - started}/150 " +
                    $"support={fixture.Exit.Name} native_upward_contact=true full_capsule=true stable={exitStable} " +
                    $"pulses={fixture.Lane.PulseCount} guards={guardEvents} HP=5 damage=0 " +
                    $"energy=1->{spent} heat=0->{raised} L=1 Space=1 dash=0 claw=0 recovery=0");
                driver.Command(InputActions.KeyboardShield, false);
                ulong released = Engine.GetPhysicsFrames();
                while (Engine.GetPhysicsFrames() - released < 100) await Tick();
                TestAssert.True(!Input.IsPhysicalKeyPressed(Key.L) && !input.Current.KeyboardShieldHeld &&
                    !shield.IsActive && !energy.IsShieldSustained && motor.ShieldMovementScale == 1f &&
                    lUp == 1 && energy.EnergyRatio > spent && energy.HeatRatio < raised &&
                    health.CurrentHealth == 5 && damageEvents == 0 && ShieldLaneSupported(player, fixture.Exit) &&
                    !fixture.Lane.GetOverlappingBodies().Contains(player),
                    $"real L-up restores guard movement and recovers energy/heat on exact safe support; " +
                    $"energy={spent}->{energy.EnergyRatio} heat={raised}->{energy.HeatRatio} L_up={lUp}");
                GD.Print($"SHIELD_LANE_COOLDOWN_OK actual_ticks={Engine.GetPhysicsFrames() - released}/100 " +
                    $"energy={spent}->{energy.EnergyRatio} heat={raised}->{energy.HeatRatio} raw_L=false sampled_L=false supported=true");
            }
            TestAssert.True(fixture.Root.RecoveryRequests == 0 && driver.AttackEdges == 0 && driver.ClawEdges == 0,
                "local shield fixture finishes without recovery or other ability inputs");
        }
        finally
        {
            try
            {
                if (subscribed) tree.PhysicsFrame -= Observe;
                health.Damaged -= damaged; health.Healed -= healed; health.Died -= died;
                shield.ShieldResolved -= Resolved;
                beforeDriverCleanup?.Invoke();
            }
            finally { driver?.Dispose(); }
        }
    }

    public static async Task RunBellGateOnly(SceneTree tree)
    {
        await AssertBellChainGateBehavior(tree);
        GD.Print("THRUSTER_BELL_GATE_OK original_saved_sector=true admitted_pre_tree_pose=true actual_door_collision=true recovery_requests_zero=true legacy_adapter_input=true whole_route=false human=false");
    }

    private static async Task AssertBellChainGateBehavior(SceneTree tree)
    {
        ReleaseAll();
        var fixture = new RangedFairnessRecoveryProbe { Name = "Task8BellChainGate" };
        PrologueSector sector = GD.Load<PackedScene>(Sectors[4].Path).Instantiate<PrologueSector>();
        CharacterBody2D player = NewPlayer();
        // Owned local gate probe, not a claim that the full canyon reached here.
        // The old (520,574) spawned through the CourtyardFloor underside.
        player.Position = new Vector2(640, 358);
        fixture.AddChild(sector);
        fixture.AddChild(player);
        var support = sector.GetNode<StaticBody2D>("Gameplay/Collision/CheckpointFloor");
        Rect2 floorBounds = ShieldLaneRectangle(support);
        var capsule = ShieldLaneCapsule(player);
        TestAssert.Equal(new Rect2(560f, 396f, 400f, 40f), floorBounds,
            "Bell gate fixture independently pins original saved checkpoint support");
        TestAssert.True(capsule.Center.X - capsule.Radius > floorBounds.Position.X &&
            capsule.Center.X + capsule.Radius < 685f && capsule.Center.Y + capsule.HalfHeight == 394f,
            "Bell fixture pre-tree full capsule is above support and strictly before actual door, not embedded");
        foreach (KillZone zone in ShieldLaneNodes(sector).OfType<KillZone>())
            zone.RoomControllerPath = zone.GetPathTo(fixture);
        tree.Root.AddChild(fixture);
        MechanicalClawController claw = player.GetNode<MechanicalClawController>("MechanicalClaw");
        PlayerMotor motor = player.GetNode<PlayerMotor>("Motor");
        BellChainMechanism chain = sector.GetNode<BellChainMechanism>(
            "Gameplay/Mechanisms/BellChain");
        GrappleSurface2D target = chain.GetNode<GrappleSurface2D>("ThreadTarget");
        var trace = new RouteTrace(motor, claw) { ExpectedTarget = target.GetInstanceId() };
        claw.ClawFrameChanged += trace.OnClawFrameChanged;
        LegacyThreadInteractionAdapter adapter = target.GetNode<LegacyThreadInteractionAdapter>(
            "LegacyThreadInteractionAdapter");
        try
        {
            await WaitForFloor(tree, player);
            CollisionShape2D door = chain.GetNode<CollisionShape2D>(chain.DoorCollisionPath);
            TestAssert.True(!door.Disabled && !chain.IsOpen && IsStandingOn(player, support) && fixture.RecoveryRequests == 0,
                "BellChain gate probe begins on exact saved checkpoint support with closed door and no recovery");
            SendAction(InputActions.MoveRight, true);
            await PhysicsFrames(tree, 90);
            SendAction(InputActions.MoveRight, false);
            bool actualDoorContact = false;
            for (int index = 0; index < player.GetSlideCollisionCount(); index++)
            {
                KinematicCollision2D contact = player.GetSlideCollision(index);
                actualDoorContact |= contact.GetCollider()?.GetInstanceId() == door.GetParent().GetInstanceId() &&
                    contact.GetNormal().X < -.99f;
            }
            TestAssert.True(actualDoorContact && player.Position.X <= 665.1f &&
                IsStandingOn(player, support) && fixture.RecoveryRequests == 0,
                "real saved Door collider, not checkpoint floor side, blocks the gate probe before interaction; " +
                $"x={player.Position.X:0.0}");
            float blockedX = player.Position.X;

            MoveMouse(target.WorldAnchor);
            SendAction(InputActions.SelectGrappleMode, true);
            await PhysicsFrames(tree, 1);
            SendAction(InputActions.SelectGrappleMode, false);
            SendMouse(MouseButton.Left, true, target.WorldAnchor);
            await PhysicsFrames(tree, 1);
            SendMouse(MouseButton.Left, false, target.WorldAnchor);
            for (int frame = 0; frame < 120 && !chain.IsOpen; frame++)
            {
                MoveMouse(target.WorldAnchor);
                await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
            }
            await PhysicsFrames(tree, 3);
            TestAssert.True(chain.IsOpen && door.Disabled && adapter.ApplicationCount == 1 &&
                trace.CastCount == 1 && trace.LatchCount == 1 &&
                trace.LatchedTarget == target.GetInstanceId(),
                "real E-mode mechanical-claw cast/latch invokes the legacy adapter and opens the gate; " +
                $"state={claw.CurrentFrame.State} casts={trace.CastCount} latches={trace.LatchCount} " +
                $"applications={adapter.ApplicationCount}");
            await DriveHorizontalTo(tree, player, 850f, 180);
            TestAssert.True(player.Position.X >= 820f,
                "opened BellChain path can be crossed into the checkpoint side");
            float crossedX = player.Position.X;
            await DriveHorizontalTo(tree, player, 640f, 180);
            TestAssert.True(player.Position.X <= 670f,
                "opened shortcut remains usable for a quick return to the mechanism side");
            TestAssert.True(fixture.RecoveryRequests == 0 && IsStandingOn(player, support),
                "gate crossing and return retain actual support without any counting-only recovery request");
            GD.Print($"TASK8_BELL_GATE_OK door_x={door.GlobalPosition.X:0.0} " +
                $"door_top={door.GlobalPosition.Y - chain.DoorHeight / 2f:0.0} " +
                $"door_bottom={door.GlobalPosition.Y + chain.DoorHeight / 2f:0.0} " +
                $"blocked_x={blockedX:0.0} casts={trace.CastCount} latches={trace.LatchCount} " +
                $"applications={adapter.ApplicationCount} crossed_x={crossedX:0.0} " +
                $"return_x={player.Position.X:0.0} exact_door_contact={actualDoorContact} recovery_requests={fixture.RecoveryRequests}");
        }
        finally
        {
            ReleaseAll();
            if (GodotObject.IsInstanceValid(claw)) claw.ClawFrameChanged -= trace.OnClawFrameChanged;
            fixture.QueueFree();
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            await PhysicsFrames(tree, 2);
        }
    }

    private static async Task OpenBellGateForSegment(SceneTree tree, CharacterBody2D player,
        PrologueSector sector)
    {
        BellChainMechanism chain = sector.GetNode<BellChainMechanism>(
            "Gameplay/Mechanisms/BellChain");
        GrappleSurface2D target = chain.GetNode<GrappleSurface2D>("ThreadTarget");
        MoveMouse(target.WorldAnchor);
        SendAction(InputActions.SelectGrappleMode, true);
        await PhysicsFrames(tree, 1);
        SendAction(InputActions.SelectGrappleMode, false);
        SendMouse(MouseButton.Left, true, target.WorldAnchor);
        await PhysicsFrames(tree, 1);
        SendMouse(MouseButton.Left, false, target.WorldAnchor);
        for (int frame = 0; frame < 120 && !chain.IsOpen; frame++)
        {
            MoveMouse(target.WorldAnchor);
            await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
        }
        await PhysicsFrames(tree, 4);
        TestAssert.True(chain.IsOpen && chain.GetNode<CollisionShape2D>(
                chain.DoorCollisionPath).Disabled,
            "temple checkpoint sample satisfies the mandatory real BellChain precondition");
    }

    public static async Task RunLegacyAscentSightlinesOnly(SceneTree tree)
    {
        ActiveTree = tree;
        await AssertAscentAnchorSightlineMatrix(tree);
        GD.Print("THRUSTER_LEGACY_SIGHTLINES_OK saved_legacy_ascent=true segments=3 samples_each=3 exact_targets=true original_launch_zones=true owned_admission=true recovery_zero=true v2_main=false human=false");
    }

    public static async Task RunLegacyDescentEntryOnly(SceneTree tree)
    {
        ActiveTree = tree;
        await RunMandatorySample(tree, LegacyTeachingSectors[1], "descent_grip_lesson", .2f);
        GD.Print("THRUSTER_LEGACY_DESCENT_ENTRY_OK original_sample_point_two=true original_budget_240=true original_margin_stable=true exact_target=true recovery_zero=true pointer_adapter=true v2_main=false human=false");
    }

    public static async Task RunLegacyDescentEntryTraceOnly(SceneTree tree)
    {
        ActiveTree = tree;
        await RunMandatorySample(tree, LegacyTeachingSectors[1], "descent_grip_lesson", .2f,
            traceGateBoundary: true);
        throw new InvalidOperationException("Boundary trace is diagnostic only, not route acceptance.");
    }

    public static async Task RunLegacyAscentHighEntryOnly(SceneTree tree)
    {
        ActiveTree = tree;
        await AssertLegacyHighPartialContactCanReachFullLanding(tree);
        foreach (float fraction in new[] { .2f, .5f, .8f })
            await RunMandatorySample(tree, LegacyTeachingSectors[3], "ascent_grapple_three", fraction);
        GD.Print("THRUSTER_LEGACY_ASCENT_HIGH_OK saved_legacy_scene=true original_samples=3 actual_support=true full_capsule_forecast=true original_budget_240=true original_margin_stable=true exact_target=true recovery_zero=true pointer_adapter=true v2_main=false human=false");
    }

    // Independent native feasibility control, NOT a replacement for the three
    // full launch/claw routes. A midflight pose/velocity is authored before tree
    // entry; subsequent motion is only the real Motor plus ordinary D/neutral.
    private static async Task AssertLegacyHighPartialContactCanReachFullLanding(SceneTree tree)
    {
        ReleaseAll();
        using var owned = new LegacyTeachingSectorFixture(LegacyTeachingSectors[3], "LegacyHighPartialContactControl");
        var segment = owned.Sector.GetNode<TraversalSegment2D>("Gameplay/TraversalContracts/ascent_grapple_three");
        CharacterBody2D player = owned.CreatePlayer(new Vector2(806.74164f, 647.04816f));
        player.Velocity = new Vector2(240f, 41f);
        ActiveInputCanvas = player;
        var motor = player.GetNode<PlayerMotor>("Motor");
        var health = player.GetNode<HealthComponent>("Health");
        var landing = ReadLegacyAssistedLanding(player, owned.Sector, segment);
        int hp = health.MaxHealth; // CurrentHealth is initialized by Ready during mounting.
        var trace = new RouteTrace(motor, player.GetNode<MechanicalClawController>("MechanicalClaw"))
            { ExpectedLandingSupport = landing };
        bool firstFloor = false, firstFloorPartial = false;
        Vector2 firstFloorPosition = default;
        int frames = 0;
        tree.Root.AddChild(owned.Root);
        try
        {
            SendAction(InputActions.MoveRight, true);
            for (; frames < 166; frames++)
            {
                await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
                trace.Observe(player, segment);
                TestAssert.True(health.CurrentHealth == hp && owned.Root.RecoveryRequests == 0 &&
                    motor.AcceptedJumpCount == 0 && motor.AcceptedDashCount == 0 &&
                    trace.Claw.CurrentFrame.CastId == 0,
                    "isolated corner control uses real free motion without damage, rescue, jump, jet or claw");
                if (!firstFloor && Task4HasFloorContact(player, landing.Body))
                {
                    firstFloor = true; firstFloorPosition = player.Position;
                    firstFloorPartial = player.Position.X < landing.Left;
                }
                bool safe = HasLandingMargin(segment.LandingZone, player.Position) &&
                    Task4HasFloorContact(player, landing.Body) && player.Position.X >= landing.Left &&
                    player.Position.X <= landing.Right;
                SendAction(InputActions.MoveRight, !safe);
                if (trace.StableLandingFrames >= StableLandingRequirement && safe && player.Velocity.Length() <= 2f) break;
            }
            TestAssert.True(firstFloor && firstFloorPartial && frames < 166 &&
                trace.StableLandingFrames >= StableLandingRequirement &&
                HasLandingMargin(segment.LandingZone, player.Position) && player.Position.X >= landing.Left &&
                player.Position.X <= landing.Right && player.Velocity.Length() <= 2f,
                $"LEGACY_HIGH_PARTIAL_CONTACT_NATIVE_RED first partial walkable contact must physically reach original full support/12/.15; " +
                $"first={firstFloor}/{firstFloorPartial}/{firstFloorPosition} final={player.Position}/{player.Velocity} " +
                $"frames={frames}/166 stable={trace.StableLandingFrames}");
            GD.Print($"LEGACY_HIGH_PARTIAL_CONTACT_NATIVE_OK first={firstFloorPosition} final={player.Position} " +
                $"frames={frames}/166 stable={trace.StableLandingFrames} full_support=true margin=15% hp={hp} " +
                "pre_tree_midflight_control=true live_pose_writes=0 complete_route=false original_three_routes_still_required=true");
            owned.AssertNoRecovery();
        }
        finally
        {
            ReleaseAll();
            owned.Root.QueueFree();
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            await PhysicsFrames(tree, 2);
        }
    }

    // Passive twelve-physics-frame cache for the original .2 high-ascent input.
    // No world queries, forecasts, input writes, waits or borrowed-wrapper disposal.
    private sealed class LegacyAscentReleaseBoundaryTrace : IDisposable
    {
        private readonly CharacterBody2D _player;
        private readonly PlayerInputReader _input;
        private readonly RouteTrace _trace;
        private readonly ThrusterPostMotorObserver[] _observers;
        private readonly List<string> _rows = new(128);
        private readonly Dictionary<string, ulong> _lastSlot = new();
        private readonly HashSet<ulong> _physics = new();
        private readonly ulong _ceilingId;
        private bool _armed, _stopped, _dumped;
        private ulong _firstPhysics, _contactPhysics;
        private string _contact = "not-yet-sampled";
        private int _omitted, _faults;

        internal LegacyAscentReleaseBoundaryTrace(Node root, CharacterBody2D player,
            PrologueSector sector, RouteTrace trace)
        {
            _player = player; _trace = trace;
            _input = player.GetNode<PlayerInputReader>("Input");
            _ceilingId = sector.GetNode<StaticBody2D>("Gameplay/Collision/CatchReturnCeilingThree").GetInstanceId();
            TestAssert.True(_input.ProcessPhysicsPriority == -300 &&
                player.GetNode("ActionModes").ProcessPhysicsPriority == -200 &&
                trace.Claw.ProcessPhysicsPriority == -100 && trace.Motor.ProcessPhysicsPriority == 0,
                "passive high-ascent observers bracket unchanged automatic Input/Claw/Motor priorities");
            _observers = new[] {
                new ThrusterPostMotorObserver { ProcessPhysicsPriority = -299,
                    Observe = () => Record("input-after", distinctSlot: true) },
                new ThrusterPostMotorObserver { ProcessPhysicsPriority = -101,
                    Observe = () => Record("claw-before", distinctSlot: true) },
                new ThrusterPostMotorObserver { ProcessPhysicsPriority = -99,
                    Observe = () => Record("claw-after", distinctSlot: true) },
                new ThrusterPostMotorObserver { ProcessPhysicsPriority = 100,
                    Observe = () => Record("motor-after", distinctSlot: true, contact: true) }
            };
            foreach (var observer in _observers) root.AddChild(observer);
            trace.Claw.ClawFrameChanged += OnClaw;
        }

        internal void Arm(int driverFrame, float existingPrediction, int existingRemaining,
            float standingY, float supportLeft, float supportRight)
        {
            if (_armed || _stopped) return;
            _armed = true; _firstPhysics = Engine.GetPhysicsFrames();
            Mark($"original-request-driver-{driverFrame}-pred-{existingPrediction:R}-remaining-{existingRemaining}" +
                $"-standing-{standingY:R}-support-{supportLeft:R}..{supportRight:R}");
        }
        internal void Mark(string marker) => Record(marker);
        private void OnClaw(MechanicalClawFrame frame, GrappleSurface2D target)
        {
            if (frame.State is MechanicalClawState.Boost or MechanicalClawState.Released)
                Record("synchronous-" + frame.State, payload: frame);
        }
        private void Record(string slot, bool distinctSlot = false, bool contact = false,
            MechanicalClawFrame? payload = null)
        {
            if (!_armed || _stopped) return;
            ulong physics = Engine.GetPhysicsFrames();
            if (physics < _firstPhysics || physics - _firstPhysics >= 12UL) return;
            if (distinctSlot && _lastSlot.TryGetValue(slot, out ulong previous) && previous == physics) return;
            if (distinctSlot) _lastSlot[slot] = physics;
            if (_rows.Count >= 128) { _omitted++; return; }
            try
            {
                _physics.Add(physics);
                if (contact)
                {
                    _contactPhysics = physics;
                    int count = _player.GetSlideCollisionCount();
                    _contact = "none";
                    if (count > 0)
                    {
                        // Borrowed completed MoveAndSlide cache: copy scalar values only.
                        KinematicCollision2D hit = _player.GetSlideCollision(0);
                        ulong collider = hit.GetColliderId();
                        _contact = $"count:{count},id:{collider},ceiling_three:{collider == _ceilingId}," +
                            $"normal:{hit.GetNormal()},position:{hit.GetPosition()},travel:{hit.GetTravel()}," +
                            $"remainder:{hit.GetRemainder()},depth:{hit.GetDepth():R}";
                    }
                }
                PlayerInputFrame sampled = _input.CurrentFrame, raw = _input.RawFrame;
                MechanicalClawFrame current = _trace.Claw.CurrentFrame;
                string eventValues = payload is { } delivered
                    ? $"payload_state={delivered.State} payload_cast={delivered.CastId} payload_lock={delivered.LockedTargetRuntimeId} " +
                      $"payload_position={delivered.CasterWorldPosition} payload_velocity={delivered.CasterVelocity} " +
                      $"payload_correction={delivered.VelocityCorrection} payload_release={delivered.ReleaseKind}"
                    : "payload=none";
                _rows.Add($"ASCENT_RELEASE_BOUNDARY slot={slot} physics={physics} offset={physics - _firstPhysics} " +
                    $"process={Engine.GetProcessFrames()} sample={_input.CurrentFrameSequence} " +
                    $"axis={sampled.MoveAxis:R} raw_axis={raw.MoveAxis:R} space={sampled.JumpPressed}/{sampled.JumpHeld} " +
                    $"raw_space={raw.JumpPressed}/{raw.JumpHeld} primary={sampled.PrimaryPressed}/{sampled.PrimaryHeld}/{sampled.PrimaryReleased} " +
                    $"raw_primary={raw.PrimaryPressed}/{raw.PrimaryHeld}/{raw.PrimaryReleased} keyboard={sampled.KeyboardActions}/{raw.KeyboardActions} " +
                    $"action_axis={Input.GetActionStrength(InputActions.MoveRight) - Input.GetActionStrength(InputActions.MoveLeft):R} " +
                    $"action_space={Input.IsActionPressed(InputActions.Jump)}/{Input.IsActionJustPressed(InputActions.Jump)}/{Input.IsActionJustReleased(InputActions.Jump)} " +
                    $"action_primary={Input.IsActionPressed(InputActions.PrimaryAction)} physical_space={Input.IsPhysicalKeyPressed(Key.Space)} mouse_left={Input.IsMouseButtonPressed(MouseButton.Left)} " +
                    $"body_position={_player.GlobalPosition} body_velocity={_player.Velocity} floor={_player.IsOnFloor()} " +
                    $"jumps={_trace.Motor.AcceptedJumpCount} driver_frame={_trace.Frames} stable={_trace.StableLandingFrames} " +
                    $"state={current.State} failure={current.Failure} cast={current.CastId} next={current.NextCastId} lock={current.LockedTargetRuntimeId} " +
                    $"anchor={current.AnchorPoint} normal={current.SurfaceNormal} dock={current.DockPoint} correction={current.VelocityCorrection} " +
                    $"release_momentum={_trace.Motor.HasGrappleReleaseMomentum} {eventValues} " +
                    $"contact_physics={_contactPhysics} contact={_contact}");
            }
            catch (Exception error)
            {
                _faults++;
                if (_rows.Count < 128) _rows.Add($"ASCENT_RELEASE_BOUNDARY observer_fault={error.GetType().Name}:{error.Message}");
            }
        }
        internal void DumpAndStop()
        {
            if (_dumped) return;
            _dumped = true; _stopped = true;
            _trace.Claw.ClawFrameChanged -= OnClaw;
            foreach (var observer in _observers) observer.Observe = null;
            foreach (string row in _rows) GD.Print(row);
            GD.Print($"ASCENT_RELEASE_BOUNDARY_END armed={_armed} physics_frames={_physics.Count}/12 " +
                $"records={_rows.Count}/128 omitted={_omitted} diagnostic_faults={_faults} ceiling_id={_ceilingId} " +
                "additional_awaits=0 world_queries=0 cached_slide_reads=true input_writes=0 diagnostic_only=true behavior_accepted=false");
        }
        public void Dispose() => DumpAndStop(); // Observer nodes remain owned by the original fixture.
    }

    // Test-owned callbacks bracket the existing controller without adding production
    // tracing APIs or writing input, model, player, geometry or collision state.
    private sealed class LegacyDescentGateBoundaryTrace : IDisposable
    {
        private readonly CharacterBody2D _player;
        private readonly PlayerInputReader _input;
        private readonly MechanicalClawController _claw;
        private readonly ThrusterPostMotorObserver _before, _after;
        private readonly List<string> _rows = new();
        private MechanicalClawFrame _previous;
        private bool _stopped, _dumped;

        internal LegacyDescentGateBoundaryTrace(Node root, CharacterBody2D player)
        {
            _player = player;
            _input = player.GetNode<PlayerInputReader>("Input");
            _claw = player.GetNode<MechanicalClawController>("MechanicalClaw");
            TestAssert.True(!root.IsInsideTree() && _input.ProcessPhysicsPriority == -300 &&
                player.GetNode("ActionModes").ProcessPhysicsPriority == -200 &&
                _claw.ProcessPhysicsPriority == -100 && player.GetNode("Motor").ProcessPhysicsPriority == 0,
                "boundary observers bracket actual saved controller after sampled input, before motor");
            _before = new() { ProcessPhysicsPriority = -101, Observe = Before };
            _after = new() { ProcessPhysicsPriority = -99, Observe = After };
            root.AddChild(_before); root.AddChild(_after);
        }

        private void Before()
        {
            if (_stopped || _rows.Count >= 64) return;
            try
            {
                _previous = _claw.CurrentFrame with { CasterWorldPosition = _player.GlobalPosition,
                    CasterVelocity = _player.Velocity };
                PlayerInputFrame input = _input.CurrentFrame;
                string ray = "not_locked", pureNext = "not_evaluated";
                bool? visible = null;
                if (_previous.State == MechanicalClawState.Travel && _claw.CurrentTarget is { } target)
                {
                    Vector2 anchor = target.UseCollisionHitPoint ? _previous.AnchorPoint : target.WorldAnchor;
                    using var query = PhysicsRayQueryParameters2D.Create(_player.GlobalPosition, anchor);
                    query.CollideWithAreas = true; query.CollideWithBodies = true;
                    query.HitFromInside = false;
                    query.CollisionMask = CombatCollisionLayers.World | CombatCollisionLayers.Enemy |
                        CombatCollisionLayers.ThreadTarget;
                    var excluded = new Godot.Collections.Array<Rid>();
                    using var excludedOwner = (Godot.Collections.Array)excluded;
                    CollectCollisionRids(_player, excluded); query.Exclude = excluded;
                    using Godot.Collections.Dictionary hit = _player.GetWorld2D().DirectSpaceState.IntersectRay(query);
                    Node collider = hit.Count > 0 ? hit["collider"].AsGodotObject() as Node : null;
                    visible = hit.Count == 0 || collider != null &&
                        MechanicalClawTargetResolver.ColliderBelongsToSurface(collider, target);
                    ray = collider == null ? "clear" : $"{collider.GetPath()}@{hit["position"].AsVector2()}";
                    // Independent replay of the pure model, NOT an observation of its
                    // controller-local 'next' or a replacement behavioural oracle.
                    if (!input.ShieldHeld && !input.AttackModePressed && !input.PrimaryPressed &&
                        !input.ClawPressed && !input.JumpPressed && !input.AttackPressed)
                    {
                        var snapshot = new MechanicalClawTargetSnapshot(target.GetInstanceId(), target.IsAvailable,
                            target.Material, anchor, visible.Value, false, _previous.SurfaceNormal);
                        pureNext = new MechanicalClawModel().Step(_previous,
                            new MechanicalClawInput(MechanicalClawCommand.None, input.MoveAxis,
                                false, _previous.DockPoint), snapshot, (float)_before.GetPhysicsProcessDeltaTime()).State.ToString();
                    }
                }
                _rows.Add($"DESCENT_GATE_BOUNDARY before physics={Engine.GetPhysicsFrames()} sample={_input.CurrentFrameSequence} " +
                    $"state={_previous.State} cast={_previous.CastId} lock={_previous.LockedTargetRuntimeId} " +
                    $"position={_player.GlobalPosition} velocity={_player.Velocity} axis={input.MoveAxis} " +
                    $"space={input.JumpPressed}/{input.JumpHeld} primary={input.PrimaryPressed} " +
                    $"claw={input.ClawPressed} shield={input.ShieldHeld} attack={input.AttackPressed}/{input.AttackModePressed} " +
                    $"los={visible} ray={ray} pure_none_next={pureNext} boundary_not_internal=true");
            }
            catch (Exception error) { _rows.Add($"DESCENT_GATE_BOUNDARY observer_fault={error.GetType().Name}:{error.Message}"); _stopped = true; }
        }

        private void After()
        {
            if (_stopped || _rows.Count >= 64) return;
            MechanicalClawFrame actual = _claw.CurrentFrame;
            _rows.Add($"DESCENT_GATE_BOUNDARY after physics={Engine.GetPhysicsFrames()} sample={_input.CurrentFrameSequence} " +
                $"prior={_previous.State} state={actual.State} failure={actual.Failure} release={actual.ReleaseKind} " +
                $"cast={actual.CastId} lock={actual.LockedTargetRuntimeId} correction={actual.VelocityCorrection} " +
                $"position={_player.GlobalPosition} velocity={_player.Velocity} boundary_not_internal=true");
            if (actual.Failure == MechanicalClawFailure.Obstructed) _stopped = true;
        }

        internal void DumpAndStop()
        {
            _stopped = true;
            if (_dumped) return;
            _dumped = true;
            foreach (string row in _rows) GD.Print(row);
            GD.Print("DESCENT_GATE_BOUNDARY_END diagnostic_only=true behavior_accepted=false observers_write_no_source_or_inputs=true");
        }
        public void Dispose()
        {
            _before.Observe = null; _after.Observe = null;
            if (GodotObject.IsInstanceValid(_before)) _before.Free();
            if (GodotObject.IsInstanceValid(_after)) _after.Free();
        }
    }

    private sealed class LegacyTeachingSectorFixture : IDisposable
    {
        internal RangedFairnessRecoveryProbe Root { get; }
        internal PrologueSector Sector { get; }
        private CharacterBody2D _player;

        internal LegacyTeachingSectorFixture(SectorCase sectorCase, string name)
        {
            Root = new() { Name = name };
            try
            {
                TestAssert.True(LegacyTeachingSectors.Any(item => item.Path == sectorCase.Path),
                    "legacy compatibility fixture requires an explicit saved legacy entry, not V2 Main");
                Sector = GD.Load<PackedScene>(sectorCase.Path).Instantiate<PrologueSector>();
                Root.AddChild(Sector);
                TestAssert.True(Sector.Name == sectorCase.Name && Sector.Transform == Transform2D.Identity,
                    "owned legacy fixture preserves exact named saved sector geometry");
                foreach (KillZone zone in ShieldLaneNodes(Sector).OfType<KillZone>())
                    zone.RoomControllerPath = zone.GetPathTo(Root);
            }
            catch { Root.Free(); throw; }
        }

        internal CharacterBody2D CreatePlayer(Vector2 start)
        {
            TestAssert.True(_player == null && start.IsFinite() && !Root.IsInsideTree(),
                "owned legacy player is created once at a finite pre-tree start");
            _player = NewPlayer(); Root.AddChild(_player); _player.Position = start;
            return _player;
        }
        internal void AssertNoRecovery() => TestAssert.Equal(0, Root.RecoveryRequests,
            "legacy teaching compatibility uses no recovery teleport or heal");
        public void Dispose()
        {
            ReleaseAll();
            if (GodotObject.IsInstanceValid(Root)) Root.Free();
        }
    }

    public static async Task RunLegacyTerrainOnly(SceneTree tree)
    {
        ActiveTree = tree;
        await AssertDenseTerrainWallLatch(tree);
        await AssertCeilingLatchBoostAndReel(tree);
        await AssertFallingSelfRescue(tree);
        GD.Print("THRUSTER_LEGACY_TERRAIN_OK saved_legacy_scene=true cases=3 first_hit_wall=true ceiling_boost=true falling_rescue=true original_budgets=true owned_admission=true recovery_zero=true pointer_adapter=true v2_main=false human=false");
    }

    private sealed class LegacyDescentTerrainFixture : IDisposable
    {
        internal RangedFairnessRecoveryProbe Root { get; }
        internal PrologueSector Sector { get; }
        internal CharacterBody2D Player { get; }

        internal LegacyDescentTerrainFixture(string name, Vector2 start)
        {
            Root = new() { Name = name };
            try
            {
                Sector = GD.Load<PackedScene>("res://scenes/prologue/sample/legacy/CanyonDescent.tscn")
                    .Instantiate<PrologueSector>();
                Root.AddChild(Sector);
                Player = NewPlayer(); Player.Position = start; Root.AddChild(Player);
                TestAssert.True(Root.Transform == Transform2D.Identity && Sector.Transform == Transform2D.Identity,
                    "legacy terrain fixture preserves saved local/global geometry before mounting");
                var pins = new[]
                {
                    ("CraneShaftWall", new Rect2(579f,160f,42f,300f), false),
                    ("FrostVentCeiling", new Rect2(620f,484f,360f,32f), false),
                    ("SelfRescueWall", new Rect2(1099f,490f,42f,260f), false),
                    ("SelfRescueCatch", new Rect2(1080f,884f,200f,32f), true),
                    ("GripLanding", new Rect2(570f,740f,300f,40f), true),
                    ("EntryShelf", new Rect2(0f,610f,280f,40f), false)
                };
                foreach (var pin in pins)
                {
                    var body = Sector.GetNodeOrNull<StaticBody2D>("Gameplay/Collision/" + pin.Item1);
                    TestAssert.True(body != null, "owned legacy terrain admits named body " + pin.Item1);
                    var shape = body.GetNodeOrNull<CollisionShape2D>("Collision");
                    TestAssert.True(shape != null && !shape.Disabled && shape.Shape is RectangleShape2D &&
                        body.Rotation == 0f && body.Scale == Vector2.One &&
                        shape.Transform == Transform2D.Identity && shape.OneWayCollision == pin.Item3 &&
                        (!pin.Item3 || shape.OneWayCollisionMargin == 16f),
                        "owned legacy rectangle retains transform and exact one-way policy " + pin.Item1);
                    var size = ((RectangleShape2D)shape.Shape).Size;
                    TestAssert.Equal(pin.Item2, new Rect2(body.Position - size / 2f, size),
                        "owned legacy saved rectangle remains unchanged " + pin.Item1);
                    var profile = body.GetNodeOrNull<GrappleSurface2D>("GrappleSurfaceProfile");
                    TestAssert.True(profile != null && profile.UseCollisionHitPoint &&
                        profile.Material == GrappleSurfaceMaterial.GripStone,
                        "legacy terrain is exact hit-point GripStone, not similarly named V2 Ice " + pin.Item1);
                }
                var capsule = Player.GetNode<CollisionShape2D>("BodyCollider").Shape as CapsuleShape2D;
                TestAssert.True(capsule != null && capsule.Radius == 20f && capsule.Height == 72f,
                    "legacy terrain preserves real player capsule dimensions");
                Rect2 bounds = new(start - new Vector2(20f,36f), new Vector2(40f,72f));
                foreach (var body in Sector.GetNode("Gameplay/Collision").GetChildren().OfType<StaticBody2D>())
                {
                    var shape = body.GetNodeOrNull<CollisionShape2D>("Collision");
                    if (shape?.Shape is not RectangleShape2D rectangle || shape.Disabled) continue;
                    TestAssert.True(!bounds.Intersects(new Rect2(body.Position + shape.Position - rectangle.Size/2f,
                        rectangle.Size)), "legacy start capsule has no saved terrain overlap: " + body.Name);
                }
                foreach (KillZone zone in ShieldLaneNodes(Sector).OfType<KillZone>())
                    zone.RoomControllerPath = zone.GetPathTo(Root);
            }
            catch { Root.Free(); throw; }
        }

        internal void AssertNoRecovery() => TestAssert.Equal(0, Root.RecoveryRequests,
            "legacy terrain compatibility uses no recovery teleport or heal");
        public void Dispose()
        {
            ReleaseAll();
            if (GodotObject.IsInstanceValid(Root)) Root.Free();
        }
    }

    public static async Task RunLegacyGripStoneOnly(SceneTree tree)
    {
        await AssertGripStoneLatchAndLanding(tree);
        await AssertGripStoneRejectsPhysicalMarginalLanding(tree);
        await AssertAttachedSecondSpaceRejectsFloorContact(tree);
        GD.Print("THRUSTER_LEGACY_GRIP_STONE_OK saved_legacy_scene=true cases=3 exact_target=true real_landing_margin_headroom=true physical_edge_negative=true attached_floor_space_negative=true recovery_zero=true v2_main=false human=false");
    }

    private sealed class LegacyGripStoneFixture : IDisposable
    {
        internal RangedFairnessRecoveryProbe Root { get; } = new() { Name = "LegacyGripStoneContracts" };
        internal PrologueSector Sector { get; }
        internal CharacterBody2D Player { get; }
        internal GrappleSurface2D Target { get; }
        internal StaticBody2D LaunchSupport { get; }
        internal StaticBody2D CatchZone { get; }

        internal LegacyGripStoneFixture()
        {
            try
            {
                Sector = GD.Load<PackedScene>("res://scenes/prologue/sample/legacy/CanyonDescent.tscn")
                    .Instantiate<PrologueSector>();
                Root.AddChild(Sector);
                Player = NewPlayer();
                Player.Position = new Vector2(170f, 514f);
                Root.AddChild(Player);
                Target = Sector.GetNodeOrNull<GrappleSurface2D>("Gameplay/GrappleSurfaces/GripStoneLesson");
                LaunchSupport = Sector.GetNodeOrNull<StaticBody2D>("Gameplay/Collision/EntryShelf");
                CatchZone = Sector.GetNodeOrNull<StaticBody2D>("Gameplay/Collision/GripStoneCatchZone");
                TestAssert.True(Target != null && LaunchSupport != null && CatchZone != null,
                    "owned legacy teaching scene admits its exact target and both named supports before mounting");
                TestAssert.Equal(new Rect2(0f, 610f, 280f, 40f), ShieldLaneRectangle(LaunchSupport),
                    "legacy teaching retains its saved launch rectangle, not current V2 route geometry");
                TestAssert.Equal(new Rect2(260f, 684f, 160f, 32f), ShieldLaneRectangle(CatchZone),
                    "legacy teaching retains its saved catch rectangle before any adversarial arrangement");
                TestAssert.True(Target.Position == new Vector2(260f, 260f) && !Target.UseCollisionHitPoint &&
                    Target.Material == GrappleSurfaceMaterial.GripStone, "legacy exact GripStone identity remains a point target");
                var capsule = ShieldLaneCapsule(Player);
                TestAssert.True(capsule.Radius == 20f && capsule.HalfHeight == 36f &&
                    capsule.Center.X - capsule.Radius > 0f && capsule.Center.X + capsule.Radius < 280f &&
                    capsule.Center.Y + capsule.HalfHeight < 610f,
                    "legacy pre-tree capsule starts wholly above and within its original launch support");
                foreach (KillZone zone in ShieldLaneNodes(Sector).OfType<KillZone>())
                    zone.RoomControllerPath = zone.GetPathTo(Root);
            }
            catch { Root.Free(); throw; }
        }

        internal void AssertNoRecovery() => TestAssert.Equal(0, Root.RecoveryRequests,
            "legacy teaching uses no counting-only recovery request, teleport or heal");

        public void Dispose()
        {
            ReleaseAll();
            if (GodotObject.IsInstanceValid(Root)) Root.Free();
        }
    }

    private static async Task AssertGripStoneLatchAndLanding(SceneTree tree)
    {
        ReleaseAll();
        using var owned = new LegacyGripStoneFixture();
        var fixture = owned.Root;
        PrologueSector sector = owned.Sector;
        CharacterBody2D player = owned.Player;
        tree.Root.AddChild(fixture);
        MechanicalClawController claw = player.GetNode<MechanicalClawController>("MechanicalClaw");
        GrappleSurface2D target = owned.Target;
        StaticBody2D launchSupport = owned.LaunchSupport;
        StaticBody2D catchZone = owned.CatchZone;
        var trace = new RouteTrace(player.GetNode<PlayerMotor>("Motor"), claw)
        {
            ExpectedTarget = target.GetInstanceId()
        };
        claw.ClawFrameChanged += trace.OnClawFrameChanged;
        try
        {
            await WaitForFloor(tree, player);
            TestAssert.True(IsStandingOn(player, launchSupport),
                "GripStone probe begins on its explicit authored launch support");
            MoveMouse(target.WorldAnchor);
            SendAction(InputActions.SelectGrappleMode, true);
            await PhysicsFrames(tree, 1);
            SendAction(InputActions.SelectGrappleMode, false);
            SendMouse(MouseButton.Left, true, target.WorldAnchor);
            await PhysicsFrames(tree, 1);
            SendMouse(MouseButton.Left, false, target.WorldAnchor);
            for (int frame = 0; frame < 120 && !trace.HasAttached; frame++)
            {
                MoveMouse(target.WorldAnchor);
                await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
            }
            TestAssert.True(trace.HasAttached && trace.LatchedTarget == trace.ExpectedTarget,
                "real GripStone E+left cast travels and latches its exact target");
            SendAction(InputActions.MoveRight, true);
            bool leftLaunchSupport = false;
            for (int frame = 0; frame < 90; frame++)
            {
                await PhysicsFrames(tree, 1);
                leftLaunchSupport |= !IsStandingOn(player, launchSupport);
            }
            TestAssert.True(leftLaunchSupport,
                "real GripStone latch pulls the player away from the original launch support");
            SendMouse(MouseButton.Left, true, target.WorldAnchor);
            await PhysicsFrames(tree, 1);
            SendMouse(MouseButton.Left, false, target.WorldAnchor);
            int stable = 0;
            for (int frame = 0; frame < 240 && stable < 3; frame++)
            {
                await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
                if (catchZone != null && player.Position.X >= catchZone.Position.X - 10f)
                    SendAction(InputActions.MoveRight, false);
                stable = player.IsOnFloor() ? stable + 1 : 0;
            }
            SendAction(InputActions.MoveRight, false);
            TestAssert.True(stable >= 3,
                $"GripStone release preserves a real safe landing; position={player.Position}");
            TestAssert.True(catchZone != null && IsSafeGripStoneCatchLanding(player, catchZone),
                "GripStone cast leaves its launch shelf and settles in the named authored " +
                "GripStoneCatchZone interior with collision-radius safety and headroom; " +
                $"landing={player.Position} catch={catchZone?.Position} " +
                $"size={(catchZone?.GetNodeOrNull<CollisionShape2D>("Collision")?.Shape as RectangleShape2D)?.Size}");
            owned.AssertNoRecovery();
            GD.Print($"TASK8_GRIPSTONE_OK casts={trace.CastCount} latches={trace.LatchCount} " +
                $"landing={player.Position} inset={PlayerCollisionRadius + GripStoneCatchSafetyMargin} " +
                $"headroom={GripStoneCatchHeadroom}");
        }
        finally
        {
            if (GodotObject.IsInstanceValid(claw)) claw.ClawFrameChanged -= trace.OnClawFrameChanged;
            ReleaseAll();
            fixture.QueueFree();
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            await PhysicsFrames(tree, 2);
        }
    }

    // Adversarial floor-contact probe: the old cumulative causality helper accepted an attached
    // cast without checking whether the player was grounded at that immediate Space edge.
    internal static async Task AssertAttachedSecondSpaceRejectsFloorContact(SceneTree tree)
    {
        ReleaseAll();
        using var owned = new LegacyGripStoneFixture();
        var fixture = owned.Root;
        CharacterBody2D player = owned.Player;
        tree.Root.AddChild(fixture);
        MechanicalClawController claw = player.GetNode<MechanicalClawController>("MechanicalClaw");
        GrappleSurface2D target = owned.Target;
        var trace = new RouteTrace(player.GetNode<PlayerMotor>("Motor"), claw)
        {
            ExpectedTarget = target.GetInstanceId()
        };
        claw.ClawFrameChanged += trace.OnClawFrameChanged;
        try
        {
            await WaitForFloor(tree, player);
            MoveMouse(target.WorldAnchor);
            SendAction(InputActions.SelectGrappleMode, true);
            await PhysicsFrames(tree, 1);
            SendAction(InputActions.SelectGrappleMode, false);
            SendMouse(MouseButton.Left, true, target.WorldAnchor);
            await PhysicsFrames(tree, 1);
            SendMouse(MouseButton.Left, false, target.WorldAnchor);
            bool observedAttachedFloor = false;
            for (int frame = 0; frame < 120 && !observedAttachedFloor; frame++)
            {
                MoveMouse(target.WorldAnchor);
                await PhysicsFrames(tree, 1);
                if (!trace.HasAttached || !player.IsOnFloor()) continue;
                bool floorEdgeAccepted = trace.TryBeginAttachedSecondSpace(player, target);
                GD.Print($"TASK8_FIX3_AIRBORNE_EDGE_PROBE state={claw.CurrentFrame.State} " +
                    $"cast={claw.CurrentFrame.CastId} left={Input.IsMouseButtonPressed(MouseButton.Left)} " +
                    $"on_floor={player.IsOnFloor()} accepted={floorEdgeAccepted}");
                TestAssert.True(!floorEdgeAccepted,
                    "an attached cumulative cast on floor must be rejected before the Space edge");
                observedAttachedFloor = true;
            }
            TestAssert.True(observedAttachedFloor,
                "adversarial real E+left fixture reaches an attached floor-contact state");
            owned.AssertNoRecovery();
        }
        finally
        {
            if (GodotObject.IsInstanceValid(claw)) claw.ClawFrameChanged -= trace.OnClawFrameChanged;
            ReleaseAll();
            fixture.QueueFree();
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        }
    }

    // Intentional historical RED: the real physical edge landing is accepted by the old
    // standing-only predicate. This runner is never added to production suites.
    internal static async Task AssertGripStoneLegacyMarginPhysicalRed(SceneTree tree)
    {
        GripStoneMarginFlight flight = await RunGripStoneMarginFlight(tree);
        GD.Print($"TASK8_FIX4_GRIPSTONE_MARGIN_RED_PHYSICAL casts={flight.CastCount} " +
            $"latches={flight.LatchCount} departed={flight.DepartedLaunchSupport} " +
            $"landing={flight.Landing} catch_left={flight.CatchLeft:0.0} " +
            $"legacy_accepted={flight.LegacyAccepted} safe_accepted={flight.SafeAccepted}");
        TestAssert.True(!flight.LegacyAccepted,
            "legacy standing-only GripStone acceptance must reject the physical marginal landing");
    }

    // Break caught: an actual E+LMB GripStone flight can settle on a collision-overlap edge that
    // the old standing-only predicate accepted although the authored radius, safety, and headroom
    // contract rejects it. The catch shelf shift is a fixture arrangement before any input; there
    // is no post-launch position reset or teleport.
    internal static async Task AssertGripStoneRejectsPhysicalMarginalLanding(SceneTree tree)
    {
        GripStoneMarginFlight flight = await RunGripStoneMarginFlight(tree);
        GD.Print($"TASK8_FIX4_GRIPSTONE_MARGIN_GREEN_PHYSICAL casts={flight.CastCount} " +
            $"latches={flight.LatchCount} departed={flight.DepartedLaunchSupport} " +
            $"landing={flight.Landing} catch_left={flight.CatchLeft:0.0} " +
            $"legacy_accepted={flight.LegacyAccepted} safe_accepted={flight.SafeAccepted}");
        TestAssert.True(flight.CastCount == 1 && flight.LatchCount == 1 &&
            flight.DepartedLaunchSupport && flight.PhysicalLanding,
            "marginal GripStone evidence uses one real cast/latch, leaves launch support, and settles physically");
        TestAssert.True(flight.LegacyAccepted && !flight.SafeAccepted,
            "the physical edge settle reproduces old acceptance while the radius-plus-margin safe oracle rejects it");
    }

    private static async Task<GripStoneMarginFlight> RunGripStoneMarginFlight(SceneTree tree)
    {
        ReleaseAll();
        using var owned = new LegacyGripStoneFixture();
        var fixture = owned.Root;
        StaticBody2D catchZone = owned.CatchZone;
        RectangleShape2D shape = catchZone.GetNode<CollisionShape2D>("Collision").Shape as
            RectangleShape2D ?? throw new InvalidOperationException("catch shelf must be rectangular");
        // Current inertia's fixed input flight reaches x≈419. Keep the capsule
        // center inside the shelf (left410) for a real upward floor contact, but
        // less than radius20 + margin12 inside: standing-only accepts; full-body
        // safety rejects. An outside center instead contacts the rounded corner.
        // This adversarial arrangement is fixed before mounting, not live motion.
        catchZone.Position += new Vector2(150f, 0f);
        CharacterBody2D player = owned.Player;
        tree.Root.AddChild(fixture);
        MechanicalClawController claw = player.GetNode<MechanicalClawController>("MechanicalClaw");
        GrappleSurface2D target = owned.Target;
        StaticBody2D launchSupport = owned.LaunchSupport;
        var trace = new RouteTrace(player.GetNode<PlayerMotor>("Motor"), claw)
        {
            ExpectedTarget = target.GetInstanceId()
        };
        claw.ClawFrameChanged += trace.OnClawFrameChanged;
        try
        {
            await WaitForFloor(tree, player);
            TestAssert.True(IsStandingOn(player, launchSupport),
                "physical margin probe begins on the authored launch support");
            MoveMouse(target.WorldAnchor);
            SendAction(InputActions.SelectGrappleMode, true);
            await PhysicsFrames(tree, 1);
            SendAction(InputActions.SelectGrappleMode, false);
            SendMouse(MouseButton.Left, true, target.WorldAnchor);
            await PhysicsFrames(tree, 1);
            SendMouse(MouseButton.Left, false, target.WorldAnchor);
            for (int frame = 0; frame < 120 && !trace.HasAttached; frame++)
            {
                MoveMouse(target.WorldAnchor);
                await PhysicsFrames(tree, 1);
            }
            TestAssert.True(trace.HasAttached && trace.LatchedTarget == trace.ExpectedTarget,
                "physical margin probe real E+left cast latches the GripStone target");
            SendAction(InputActions.MoveRight, true);
            bool departed = false;
            for (int frame = 0; frame < 90; frame++)
            {
                await PhysicsFrames(tree, 1);
                departed |= !IsStandingOn(player, launchSupport);
            }
            SendMouse(MouseButton.Left, true, target.WorldAnchor);
            await PhysicsFrames(tree, 1);
            SendMouse(MouseButton.Left, false, target.WorldAnchor);
            int stable = 0;
            for (int frame = 0; frame < 240 && stable < 3; frame++)
            {
                await PhysicsFrames(tree, 1);
                if (player.Position.X >= 342f)
                    SendAction(InputActions.MoveRight, false);
                stable = player.IsOnFloor() ? stable + 1 : 0;
            }
            SendAction(InputActions.MoveRight, false);
            float catchLeft = catchZone.Position.X - shape.Size.X / 2f;
            bool legacyAccepted = IsStandingOn(player, catchZone);
            bool safeAccepted = IsSafeGripStoneCatchLanding(player, catchZone);
            bool exactCatchContact = false;
            var contacts = new List<string>();
            for (int index = 0; index < player.GetSlideCollisionCount(); index++)
            {
                KinematicCollision2D contact = player.GetSlideCollision(index);
                contacts.Add($"collider={contact.GetCollider()?.GetInstanceId() ?? 0UL}/normal={contact.GetNormal()}/point={contact.GetPosition()}");
                exactCatchContact |= contact.GetCollider()?.GetInstanceId() == catchZone.GetInstanceId() &&
                    contact.GetNormal().Dot(Vector2.Up) > .99f;
            }
            GD.Print($"GRIPSTONE_MARGIN_CONTACT_DIAGNOSTIC position={player.Position} floor={player.IsOnFloor()} " +
                $"stable={stable} catch_id={catchZone.GetInstanceId()} left={catchLeft} exact_up={exactCatchContact} contacts=[{string.Join(';', contacts)}]");
            owned.AssertNoRecovery();
            return new GripStoneMarginFlight(trace.CastCount, trace.LatchCount, departed,
                stable >= 3 && player.IsOnFloor() && exactCatchContact,
                player.Position, catchLeft, legacyAccepted, safeAccepted);
        }
        finally
        {
            if (GodotObject.IsInstanceValid(claw)) claw.ClawFrameChanged -= trace.OnClawFrameChanged;
            ReleaseAll();
            fixture.QueueFree();
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            await PhysicsFrames(tree, 2);
        }
    }

    private static bool IsStandingOn(CharacterBody2D player, StaticBody2D body)
    {
        if (body.GetNodeOrNull<CollisionShape2D>("Collision")?.Shape is not RectangleShape2D shape)
            return false;
        float top = body.Position.Y - shape.Size.Y / 2f;
        return player.Position.X >= body.Position.X - shape.Size.X / 2f - 8f &&
            player.Position.X <= body.Position.X + shape.Size.X / 2f + 8f &&
            MathF.Abs(player.Position.Y - (top - 36f)) <= 10f;
    }

    private static bool IsSafeGripStoneCatchLanding(CharacterBody2D player, StaticBody2D catchZone)
    {
        if (!IsStandingOn(player, catchZone) ||
            catchZone.GetNodeOrNull<CollisionShape2D>("Collision")?.Shape is not RectangleShape2D shape)
            return false;
        Rect2 footprint = new(catchZone.Position - shape.Size / 2f, shape.Size);
        float inset = PlayerCollisionRadius + GripStoneCatchSafetyMargin;
        bool inside = player.Position.X >= footprint.Position.X + inset &&
            player.Position.X <= footprint.End.X - inset;
        Rect2 headroom = new(player.Position.X - PlayerCollisionRadius,
            player.Position.Y - PlayerCollisionHalfHeight - GripStoneCatchHeadroom,
            PlayerCollisionRadius * 2f, GripStoneCatchHeadroom);
        bool blockedAbove = catchZone.GetParent()?.GetChildren().OfType<StaticBody2D>()
            .Where(body => body != catchZone)
            .Select(body => body.GetNodeOrNull<CollisionShape2D>("Collision")?.Shape is RectangleShape2D other
                ? new Rect2(body.Position - other.Size / 2f, other.Size)
                : new Rect2())
            .Any(bounds => bounds.Size != Vector2.Zero && bounds.Intersects(headroom)) == true;
        return inside && !blockedAbove;
    }

    private static async Task AssertOptionalPassAndSkip(SceneTree tree, SectorCase sectorCase,
        string optionalName)
    {
        foreach (string route in new[] { "Pass", "Skip" })
        foreach (bool reverse in new[] { false, true })
        {
            ReleaseAll();
            using var owned = new LegacyTeachingSectorFixture(sectorCase,
                $"Task8Optional_{optionalName}_{route}_{reverse}");
            var fixture = owned.Root;
            PrologueSector sector = owned.Sector;
            Node path = sector.GetNodeOrNull($"Gameplay/OptionalPaths/{optionalName}");
            Marker2D start = path?.GetNodeOrNull<Marker2D>(reverse ? $"{route}End" : $"{route}Start");
            Marker2D end = path?.GetNodeOrNull<Marker2D>(reverse ? $"{route}Start" : $"{route}End");
            TestAssert.True(path != null && start != null && end != null && start.Position.IsFinite() && end.Position.IsFinite(),
                "owned legacy optional fixture admits exact forward/reverse marker pair before mounting");
            CharacterBody2D player = owned.CreatePlayer(start.Position);
            tree.Root.AddChild(fixture);
            try
            {
                await WaitForFloor(tree, player);
                await DriveToMarker(tree, player, end.Position, 220);
                TestAssert.True(player.IsOnFloor() && player.Position.DistanceTo(end.Position) <= 52f,
                    $"{optionalName} {route.ToLowerInvariant()} route is physically traversable " +
                    $"{(reverse ? "back to mainline" : "forward")} without a one-way drop; " +
                    $"position={player.Position} target={end.Position} on_floor={player.IsOnFloor()}");
                owned.AssertNoRecovery();
            }
            finally
            {
                ReleaseAll();
                fixture.QueueFree();
                await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
                await PhysicsFrames(tree, 2);
            }
        }
        GD.Print($"TASK8_OPTIONAL_ROUTE_OK sector={sectorCase.Name} path={optionalName} " +
            "pass=True skip=True returnable=True");
    }

    private static async Task DriveToMarker(SceneTree tree, CharacterBody2D player,
        Vector2 target, int budget)
    {
        for (int frame = 0; frame < budget &&
            (!player.IsOnFloor() || player.Position.DistanceTo(target) > 42f); frame++)
        {
            float dx = target.X - player.Position.X;
            Input.ActionPress(dx < 0 ? InputActions.MoveLeft : InputActions.MoveRight);
            if (player.IsOnFloor() && target.Y < player.Position.Y - 18f)
                Input.ActionPress(InputActions.Jump);
            await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
            if (!player.IsOnFloor() || player.Velocity.Y < -80f) Input.ActionRelease(InputActions.Jump);
        }
        ReleaseAll();
        await PhysicsFrames(tree, 4);
    }

    private static async Task DriveHorizontalTo(SceneTree tree, CharacterBody2D player,
        float targetX, int budget)
    {
        StringName action = targetX < player.Position.X ? InputActions.MoveLeft : InputActions.MoveRight;
        SendAction(action, true);
        for (int frame = 0; frame < budget && MathF.Abs(player.Position.X - targetX) > 24f; frame++)
            await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
        SendAction(action, false);
        await PhysicsFrames(tree, 3);
    }

    public static async Task RunTempleTriggerAdmissionOnly(SceneTree tree)
    {
        ActiveTree = tree;
        await AssertTempleArenaLocksOnlyAfterTrigger(tree);
        GD.Print("THRUSTER_TEMPLE_TRIGGER_ADMISSION_OK saved_v2_scene=true controlled_in_memory_progress=true quota_preserved=true original_waits=true real_body_crossing=true legacy_action_adapter=true full_route=false human=false");
    }

    private static async Task AssertTempleArenaLocksOnlyAfterTrigger(SceneTree tree)
    {
        ReleaseAll();
        var fixture = new Node2D { Name = "Task8TempleBossTrigger" };
        PrologueSector sector = GD.Load<PackedScene>(Sectors[4].Path).Instantiate<PrologueSector>();
        CharacterBody2D player = NewPlayer();
        Marker2D start = sector.GetNode<Marker2D>("Gameplay/BossApproachStart");
        player.Position = start.Position;
        fixture.AddChild(sector);
        fixture.AddChild(player);
        var trigger = sector.GetNode<Area2D>("Gameplay/ActivationTrigger");
        bool playerEntered = false;
        ulong enteredBodyId = 0, enteredPhysics = 0;
        void ObserveBody(Node2D body)
        {
            if (body != player) return;
            playerEntered = true; enteredBodyId = body.GetInstanceId(); enteredPhysics = Engine.GetPhysicsFrames();
        }
        try
        {
            // This positive trigger fixture needs current V2 admission metadata.
            // No save restore, disk write, quota exemption or direct Begin is used.
            var progress = new PrologueProgress();
            foreach (string id in new[] {
                "canyon.v2.fragment.entrance", "canyon.v2.fragment.rift",
                "canyon.v2.fragment.abyss-floor", "canyon.v2.fragment.gear-gallery",
                "canyon.v2.fragment.ascent-mid", "canyon.v2.fragment.courtyard" })
                TestAssert.True(progress.TryCollectFragment(id), "controlled Boss fixture collects one known unique fragment");
            TestAssert.True(progress.ActivateMechanism(CanyonWorldLayout.CourtyardSealMechanismId) &&
                progress.OpenBellChainShortcut(), "controlled Boss fixture establishes both original prerequisites");
            BossAdmissionResult admission = BossAdmissionModel.Evaluate(progress);
            BossArenaController arena = sector.GetNode<BossArenaController>("Gameplay/BossArenaController");
            TestAssert.True(arena.RequiresFragmentAdmission && progress.CollectedFragmentIds.Count == 6 &&
                !progress.LegacyCollectionQuotaExempt && !progress.BossDefeated && admission.Allowed &&
                admission.MissingFragments == 0 && admission.CourtyardReady && admission.BellChainReady,
                "positive V2 Boss trigger test is admitted without relaxing current quota or prerequisites");
            arena.BindRuntime(progress, null);
            trigger.BodyEntered += ObserveBody;
            tree.Root.AddChild(fixture);
            GD.Print("TASK8_BOSS_TRIGGER_CONTROLLED_ADMISSION known_fragments=6 courtyard=true bell=true " +
                "quota_exempt=false boss_defeated=false bound_before_tree=true save_io=false");
            await PhysicsFrames(tree, 3);
            CollisionShape2D entry = sector.GetNode<CollisionShape2D>(
                "Gameplay/Doors/Entry/Collision");
            GD.Print($"TASK8_BOSS_TRIGGER_PREFLIGHT physics={Engine.GetPhysicsFrames()} " +
                $"position={player.Position} active={arena.IsEncounterActive} " +
                $"entry_disabled={entry.Disabled} requires_admission={arena.RequiresFragmentAdmission}");
            TestAssert.True(!arena.IsEncounterActive && entry.Disabled && !playerEntered,
                "temple boss entry remains open before the player crosses the activation trigger");
            await DriveToMarker(tree, player,
                trigger.Position + new Vector2(36, 0), 180);
            await PhysicsFrames(tree, 3);
            TestAssert.True(arena.IsEncounterActive && !entry.Disabled && playerEntered && enteredBodyId == player.GetInstanceId(),
                "temple boss arena locks only after a real player body crosses the trigger; " +
                $"position={player.Position} active={arena.IsEncounterActive} " +
                $"entry_disabled={entry.Disabled}");
            GD.Print($"TASK8_BOSS_TRIGGER_OK position={player.Position} " +
                $"active={arena.IsEncounterActive} entry_disabled={entry.Disabled} " +
                $"actual_player_body={enteredBodyId} entered_physics={enteredPhysics}");
        }
        finally
        {
            if (GodotObject.IsInstanceValid(trigger)) trigger.BodyEntered -= ObserveBody;
            ReleaseAll();
            if (fixture.IsInsideTree()) fixture.QueueFree(); else fixture.Free();
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            await PhysicsFrames(tree, 2);
        }
    }

    private static CharacterBody2D NewPlayer()
    {
        CharacterBody2D player = GD.Load<PackedScene>("res://scenes/player/Player.tscn")
            .Instantiate<CharacterBody2D>();
        player.Name = "Task8TraversalPlayer";
        player.AddToGroup("player");
        player.GetNode<Camera2D>("Camera2D").Enabled = false;
        return player;
    }

    private static async Task WaitForFloor(SceneTree tree, CharacterBody2D player)
    {
        ActiveInputCanvas = player;
        int stable = 0;
        for (int frame = 0; frame < 60 && stable < 3; frame++)
        {
            await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
            stable = player.IsOnFloor() ? stable + 1 : 0;
        }
        TestAssert.True(stable >= 3, $"physical launch settles on real collision at {player.Position}");
    }

    private static (StaticBody2D Body, float StandingY, float Left, float Right) ReadLegacyAssistedLanding(
        CharacterBody2D player, PrologueSector sector, TraversalSegment2D segment)
    {
        string supportName = segment.SegmentId.ToString() switch
        {
            "descent_grip_lesson" => "GripLanding",
            "ascent_grapple_one" => "LandingOne",
            "ascent_grapple_two" => "LandingTwo",
            "ascent_grapple_three" => "LandingThree",
            _ => throw new InvalidOperationException("No admitted legacy assisted support for " + segment.SegmentId)
        };
        var body = sector.GetNode<StaticBody2D>("Gameplay/Collision/" + supportName);
        var collision = body.GetNode<CollisionShape2D>("Collision");
        TestAssert.True(collision is { Disabled: false, OneWayCollision: true, Shape: RectangleShape2D } &&
            collision.OneWayCollisionMargin == 16f,
            "legacy assisted forecast retains exact enabled saved one-way support " + supportName);
        Transform2D pose = ShieldLanePose(collision);
        TestAssert.True(pose.X.IsEqualApprox(Vector2.Right) && pose.Y.IsEqualApprox(Vector2.Down),
            "legacy assisted forecast requires actual translation-only support");
        var rectangle = (RectangleShape2D)collision.Shape;
        Rect2 bounds = new(pose.Origin - rectangle.Size / 2f, rectangle.Size);
        var capsule = ShieldLaneCapsule(player);
        Vector2 offset = capsule.Center - player.GlobalPosition;
        float standingY = bounds.Position.Y - capsule.HalfHeight - offset.Y;
        float left = Mathf.Max(segment.LandingZone.Position.X,
            bounds.Position.X + capsule.Radius + player.SafeMargin - offset.X);
        float right = Mathf.Min(segment.LandingZone.End.X,
            bounds.End.X - capsule.Radius - player.SafeMargin - offset.X);
        TestAssert.True(bounds.HasArea() && left < right && standingY > segment.LandingZone.Position.Y &&
            standingY < segment.LandingZone.End.Y,
            "actual saved full-capsule support intersects unchanged logical landing zone");
        return (body, standingY, left, right);
    }

    private static float PredictLandingX(CharacterBody2D player, PlayerMotor motor,
        float landingY, float direction, float stopAtX, int maxSteps)
    {
        return PredictLandingX(player.Position, player.Velocity, motor, landingY, direction, stopAtX, maxSteps,
            motor.HasGrappleReleaseMomentum && player.Velocity.Y < 0f, direction);
    }

    private static float PredictBoostLandingX(CharacterBody2D player, PlayerMotor motor,
        MechanicalClawFrame frame, float swingAxis, float landingY, float direction, float stopAtX, int maxSteps,
        LegacyBoostCorridor corridor = null)
    {
        frame = frame with { CasterWorldPosition = player.GlobalPosition, CasterVelocity = player.Velocity };
        MechanicalClawSettings settings = MechanicalClawSettings.Default;
        Vector2 toAnchor = frame.CasterWorldPosition.DirectionTo(frame.AnchorPoint);
        Vector2 tangent = new(-toAnchor.Y, toAnchor.X);
        float projected = frame.CasterVelocity.Dot(tangent);
        float requested = Mathf.Abs(swingAxis) > .0001f ? swingAxis : Mathf.Sign(projected);
        float boostDirection = Mathf.IsZeroApprox(requested) ? 1f : Mathf.Sign(requested);
        Vector2 released = player.Velocity + Vector2.Up * settings.BoostUpwardSpeed +
            tangent * (boostDirection * settings.BoostTangentialSpeed);
        Vector2 outwardNormal = frame.SurfaceNormal.IsZeroApprox()
            ? frame.AnchorPoint.DirectionTo(frame.CasterWorldPosition)
            : frame.SurfaceNormal.Normalized();
        float inward = released.Dot(outwardNormal);
        if (inward < 0f) released -= outwardNormal * inward;
        if (released.Length() > settings.ReleaseSpeedLimit)
            released = released.Normalized() * settings.ReleaseSpeedLimit;
        return PredictLandingX(player.Position, released, motor, landingY, direction, stopAtX, maxSteps,
            protectGrappleRise: released.Y < 0f, releaseAxis: swingAxis, corridor);
    }

    private static float PredictLandingX(Vector2 position, Vector2 velocity, PlayerMotor motor,
        float landingY, float direction, float stopAtX, int maxSteps, bool protectGrappleRise, float releaseAxis,
        LegacyBoostCorridor corridor = null)
    {
        TestAssert.True(!motor.IsDashing, "legacy assisted no-jet forecast must not simulate an active burn");
        var model = new PlayerMotionModel(motor.MotionSettings);
        var state = new PlayerMotionState { Velocity = velocity, GrappleRiseProtected = protectGrappleRise };
        MobilityModuleSettings module = motor.ThrusterState.Module;
        bool reachedLandingHeight = position.Y <= landingY;
        corridor?.Begin();
        float seconds = 1f / Engine.PhysicsTicksPerSecond;
        for (int frame = 0; frame < maxSteps; frame++)
        {
            // Match the existing post-release input policy: keep its current
            // direction for the release sample, then neutral beyond target centre.
            float axis = frame == 0 ? releaseAxis : (direction > 0f && position.X >= stopAtX ||
                direction < 0f && position.X <= stopAtX) ? 0f : direction;
            var sample = new PlayerInputFrame(axis, new Vector2(direction, 0f), false, false, false, false, false);
            state.Velocity = model.StepFree(state, sample, false, seconds, module,
                Vector2.Zero, 0f, false, false).Velocity;
            if (state.Velocity.Y >= 0f) state.GrappleRiseProtected = false;
            Vector2 travel = state.Velocity * seconds;
            if (corridor != null && corridor.Blocks(frame, position, travel)) return float.NaN;
            if (corridor is { HasWalkableArrival: true })
                return corridor.PreviewOriginalGroundCorrection(state.Velocity, maxSteps - frame - 1)
                    ? corridor.ArrivalPosition.X : float.NaN;
            position += travel;
            reachedLandingHeight |= position.Y <= landingY;
            if (reachedLandingHeight && state.Velocity.Y >= 0 && position.Y >= landingY && corridor == null)
                return position.X;
        }
        return float.NaN;
    }

    // Optional old high-ascent planner query, never a movement owner. The only
    // owned RefCounted is explicitly created here; returned body-cache wrappers
    // and World/Space objects are neither acquired nor disposed.
    private sealed class LegacyBoostCorridor : IDisposable
    {
        private readonly CharacterBody2D _player;
        private readonly PlayerMotor _motor;
        private readonly ulong _supportId;
        private readonly float _left, _right, _standingY, _safeLeft, _safeRight;
        private readonly KinematicCollision2D _ownedProbe = new();
        internal int Queries, Candidates, BlockedCandidates;
        internal ulong LastBlockedId;
        internal int LastBlockedStep;
        internal Vector2 LastFrom, LastMotion, LastNormal;
        internal bool HasWalkableArrival, GroundCorrectionAdmitted;
        internal Vector2 ArrivalPosition, ArrivalNormal;
        internal int GroundCorrectionFrames;
        internal float GroundCompletionX;
        internal LegacyBoostCorridor(CharacterBody2D player, PlayerMotor motor,
            StaticBody2D support, float standingY, float left, float right, Rect2 zone)
        {
            _player = player; _motor = motor; _supportId = support.GetInstanceId();
            _left = left; _right = right; _standingY = standingY;
            _safeLeft = zone.Position.X + zone.Size.X * LandingMarginRatio;
            _safeRight = zone.End.X - zone.Size.X * LandingMarginRatio;
            TestAssert.True(player.Position.IsEqualApprox(player.GlobalPosition),
                "legacy corridor uses the admitted identity fixture's actual global body transform");
        }
        internal void Begin()
        {
            Candidates++; LastBlockedId = 0; HasWalkableArrival = false;
            GroundCorrectionAdmitted = false; GroundCorrectionFrames = 0;
        }
        internal bool Blocks(int futureStep, Vector2 position, Vector2 motion)
        {
            Transform2D from = _player.GlobalTransform;
            from.Origin = position;
            Queries++;
            if (!_player.TestMove(from, motion, _ownedProbe, _player.SafeMargin, true)) return false;
            ulong collider = _ownedProbe.GetColliderId();
            Vector2 normal = _ownedProbe.GetNormal();
            float nextX = position.X + motion.X;
            // Whole-capsule native query may first meet the rounded edge before
            // the centre is fully over the platform. Admit only a walkable
            // actual up-normal, then separately budget original D/neutral to
            // full support plus the unchanged final margin/stability contract.
            if (collider == _supportId && motion.Y > 0f &&
                normal.Y < -Mathf.Cos(_player.FloorMaxAngle))
            {
                HasWalkableArrival = true;
                ArrivalPosition = position + _ownedProbe.GetTravel();
                ArrivalNormal = normal;
                return false;
            }
            BlockedCandidates++;
            LastBlockedId = collider; LastBlockedStep = futureStep;
            LastFrom = position; LastMotion = motion; LastNormal = normal;
            return true;
        }
        internal bool PreviewOriginalGroundCorrection(Vector2 impactVelocity, int remaining)
        {
            // A nominal support-plane preview, not a live snap/teleport. The
            // independent native control proves the rounded-edge roll-on; the
            // three actual launch routes remain the acceptance oracle.
            var model = new PlayerMotionModel(_motor.MotionSettings);
            MobilityModuleSettings module = _motor.ThrusterState.Module;
            var state = new PlayerMotionState { Velocity = new Vector2(impactVelocity.Slide(ArrivalNormal).X, 0f),
                WasOnFloor = true };
            float x = ArrivalPosition.X;
            float seconds = 1f / Engine.PhysicsTicksPerSecond;
            for (int step = 0; step + StableLandingRequirement < remaining; step++)
            {
                float axis = x < _safeLeft ? 1f : x > _safeRight ? -1f : 0f;
                state.Velocity = model.StepFree(state,
                    new PlayerInputFrame(axis, Vector2.Right, false, false, false, false, false),
                    true, seconds, module, Vector2.Zero, 0f, false, false).Velocity;
                Vector2 motion = new(state.Velocity.X * seconds, 0f);
                Transform2D from = _player.GlobalTransform;
                from.Origin = new Vector2(x, _standingY);
                Queries++;
                if (_player.TestMove(from, motion, _ownedProbe, _player.SafeMargin, true) &&
                    !(_ownedProbe.GetColliderId() == _supportId && _ownedProbe.GetNormal().Y < -.99f))
                    return false;
                x += motion.X;
                if (x >= _left && x <= _right && x >= _safeLeft && x <= _safeRight &&
                    Mathf.Abs(state.Velocity.X) <= 2f)
                {
                    GroundCorrectionAdmitted = true;
                    GroundCorrectionFrames = step + 1;
                    GroundCompletionX = x;
                    return true;
                }
            }
            return false;
        }
        public void Dispose() => _ownedProbe.Dispose();
    }

    private static async Task PhysicsFrames(SceneTree tree, int count)
    {
        for (int index = 0; index < count; index++)
        {
            await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
            Task4FrameObserver?.Invoke();
        }
    }

    private static void SendAction(StringName action, bool pressed)
    {
        if(KeyboardRouteInputDriver.Current!=null)
        { KeyboardRouteInputDriver.Current.Command(action,pressed); return; }
        DispatchInputEvent(new InputEventAction
        {
            Action = action,
            Pressed = pressed,
            Strength = pressed ? 1f : 0f
        });
    }

    private static void SendMouse(MouseButton button, bool pressed, Vector2 position)
    {
        if(KeyboardRouteInputDriver.Current!=null)
        { KeyboardRouteInputDriver.Current.PointerButton(button,pressed,(CharacterBody2D)ActiveInputCanvas,position); return; }
        Vector2 viewportPosition = ToViewport(position);
        MoveMouse(position);
        DispatchInputEvent(new InputEventMouseButton
        {
            ButtonIndex = button,
            Pressed = pressed,
            Position = viewportPosition,
            GlobalPosition = viewportPosition,
            ButtonMask = pressed ? button == MouseButton.Left
                ? MouseButtonMask.Left
                : MouseButtonMask.Right
                : (MouseButtonMask)0
        });
    }

    private static void MoveMouse(Vector2 position)
    {
        if(KeyboardRouteInputDriver.Current!=null)
        { KeyboardRouteInputDriver.Current.AimAt((CharacterBody2D)ActiveInputCanvas,position); return; }
        Vector2 viewportPosition = ToViewport(position);
        Viewport viewport = ResolveInputViewport();
        DispatchMouseMotion(viewport, viewportPosition);
    }

    public static async Task AssertViewportMouseEventPropagation(SceneTree tree,
        PlayerInputReader inputReader, PrologueAimReticle reticle)
    {
        // This display-only proof uses the same MouseMotion event route as a real mouse:
        // Input.ParseInputEvent publishes InputMap state and the concrete Viewport.PushInput
        // delivers the same event to the production PlayerInputReader. It intentionally does
        // not demand an OS cursor warp, because that is host-window-manager dependent. Neither
        // this helper nor the adapter writes player, grapple, model, or aim state directly.
        ActiveTree = tree;
        TestAssert.True(inputReader != null && inputReader.IsInsideTree(),
            "Task6 input proof requires the production PlayerInputReader in the live scene");
        TestAssert.True(reticle != null && reticle.IsInsideTree(),
            "Task6 input proof requires the visible live aim reticle");
        Viewport viewport = inputReader.GetViewport();
        TestAssert.True(viewport != null && viewport.IsInsideTree(),
            "Task6 input proof has a live target viewport for mouse-event dispatch");

        DispatchMouseMotion(viewport, Vector2.Zero);
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
        TestAssert.True(inputReader.HasObservedMouseMotion &&
            inputReader.LastObservedMouseViewportPosition.IsEqualApprox(Vector2.Zero),
            "Task6 production input adapter must observe the event baseline (0, 0)");
        ulong baselineSequence = inputReader.MouseMotionSequence;

        Vector2 intended = new(1120f, 620f);
        DispatchMouseMotion(viewport, intended);
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        Vector2 sampled = inputReader.LastObservedMouseViewportPosition;
        TestAssert.True(inputReader.MouseMotionSequence > baselineSequence && sampled.IsEqualApprox(intended),
            $"Task6 production input adapter must observe (1120, 620), sampled={sampled} " +
            $"sequence={inputReader.MouseMotionSequence}");
        TestAssert.True(reticle.CurrentFrame.Visible && reticle.Visible &&
            reticle.CurrentFrame.ScreenPosition.IsEqualApprox(intended),
            $"Task6 visible reticle must bind the adapter target; reticle={reticle.CurrentFrame.ScreenPosition} " +
            $"sampled={sampled}");

        GD.Print($"TASK6_INPUT_VIEWPORT_BASELINE_OK sampled=(0, 0) events={ViewportInputTransport}");
        GD.Print("TASK6_INPUT_VIEWPORT_SAMPLE_OK sampled=(1120, 620) intended=(1120, 620) " +
            $"events={ViewportInputTransport}");
        GD.Print("TASK6_INPUT_RETICLE_BINDING_OK sampled=(1120, 620) reticle=(1120, 620) " +
            $"adapter=True reticle_visible=True events={ViewportInputTransport}");
    }

    private static void DispatchMouseMotion(Viewport viewport, Vector2 position)
    {
        // Generic headless teardown may release a held mouse action before a fixture mounts a
        // viewport. Input.ParseInputEvent still clears that InputMap action; display evidence
        // calls AssertViewportMouseEventPropagation above and retains its separate live
        // viewport/adapter/reticle hard assertions before it certifies any visible aim.
        DispatchInputEvent(new InputEventMouseMotion
        {
            Position = position,
            GlobalPosition = position
        }, viewport);
    }

    private static void DispatchInputEvent(InputEvent inputEvent, Viewport targetViewport = null)
    {
        // Input.ParseInputEvent keeps action state available to PlayerInputReader; PushInput
        // routes the exact same event to the concrete display viewport. Keeping both in one
        // helper prevents a synthetic test frame from bypassing the real input path.
        Input.ParseInputEvent(inputEvent);
        Viewport viewport = targetViewport ?? ResolveInputViewport();
        if (viewport != null && viewport.IsInsideTree())
            viewport.PushInput(inputEvent, true);
        Input.FlushBufferedEvents();
    }

    private static Viewport ResolveInputViewport()
    {
        if (GodotObject.IsInstanceValid(ActiveInputCanvas) && ActiveInputCanvas.IsInsideTree())
            return ActiveInputCanvas.GetViewport();
        return ActiveTree?.Root?.GetViewport();
    }

    private static Vector2 ToViewport(Vector2 worldPosition)
    {
        if (GodotObject.IsInstanceValid(ActiveInputCanvas) && ActiveInputCanvas.IsInsideTree())
            return ActiveInputCanvas.GetViewport().GetFinalTransform() *
                (ActiveInputCanvas.GetGlobalTransformWithCanvas() *
                    ActiveInputCanvas.ToLocal(worldPosition));
        return worldPosition;
    }

    private static void ReleaseAll()
    {
        if(KeyboardRouteInputDriver.Current!=null)
        { KeyboardRouteInputDriver.Current.ReleaseAll(); return; }
        Input.ActionRelease(InputActions.MoveLeft);
        Input.ActionRelease(InputActions.MoveRight);
        Input.ActionRelease(InputActions.MoveUp);
        Input.ActionRelease(InputActions.MoveDown);
        Input.ActionRelease(InputActions.Jump);
        Input.ActionRelease(InputActions.Dash);
        Input.ActionRelease(InputActions.SelectAttackMode);
        Input.ActionRelease(InputActions.SelectGrappleMode);
        Input.ActionRelease(InputActions.PrimaryAction);
        Input.ActionRelease(InputActions.Shield);
        SendMouse(MouseButton.Left, false, Vector2.Zero);
        SendMouse(MouseButton.Right, false, Vector2.Zero);
    }

    private sealed record SectorCase(string Name, string Path);

    private sealed record GripStoneMarginFlight(int CastCount, int LatchCount,
        bool DepartedLaunchSupport, bool PhysicalLanding, Vector2 Landing, float CatchLeft,
        bool LegacyAccepted, bool SafeAccepted);

    private sealed record CatchReturnCase(SectorCase Sector, string SegmentId, string CatchName,
        NodePath TargetPath, Vector2 AimLocal, bool TerrainReturn);

    private readonly record struct RayHit(Node Collider, Vector2 Position);

    private sealed partial class ReleaseVelocitySampler : Node
    {
        public CharacterBody2D Player { get; init; }
        public bool Captured { get; private set; }
        public Vector2 Velocity { get; private set; }
        private bool _armed;

        public void Arm() => _armed = true;

        public override void _PhysicsProcess(double delta)
        {
            if (!_armed || Captured) return;
            Velocity = Player.Velocity;
            Captured = true;
            SetPhysicsProcess(false);
        }
    }

    private sealed class RouteTrace
    {
        private readonly HashSet<ulong> _castIds = new();
        private readonly HashSet<ulong> _latchIds = new();

        public RouteTrace(PlayerMotor motor, MechanicalClawController claw)
        {
            Motor = motor;
            Claw = claw;
        }

        public PlayerMotor Motor { get; }
        public MechanicalClawController Claw { get; }
        public int Frames { get; set; }
        public bool LeftFloor { get; private set; }
        public int StableLandingFrames { get; private set; }
        public int CastCount => _castIds.Count;
        public int LatchCount => _latchIds.Count;
        public bool HasAttached { get; private set; }
        public ulong ExpectedTarget { get; set; }
        public (StaticBody2D Body, float StandingY, float Left, float Right)? ExpectedLandingSupport { get; set; }
        public ulong LatchedTarget { get; private set; }
        public Vector2 LatchedAnchor { get; private set; }
        public float SecondJumpVelocityBefore { get; set; }
        public int SecondJumpAcceptedCountBefore { get; set; }
        public bool SecondAirJumpAccepted { get; set; }
        public bool SecondJumpAttempted { get; set; }
        public bool SecondJumpMeasured { get; set; }
        public bool AttachedSecondSpaceCausalityObserved { get; set; }
        public bool AttachedSecondSpaceAirborneAtEdge { get; private set; }
        public MechanicalClawFailure ObservedFailure { get; private set; }
        private ulong AttachedSecondSpaceCast { get; set; }
        private bool AttachedSecondSpaceCurrentState { get; set; }
        public bool BoostFromSecondSpaceObserved { get; private set; }
        public bool PrimaryReleasedBeforeSecondSpace { get; private set; }
        public bool ReleasedFromAttachedSecondSpace { get; private set; }
        private bool SecondSpaceInputDispatched { get; set; }

        public void OnClawFrameChanged(MechanicalClawFrame frame, GrappleSurface2D target)
        {
            if (frame.Failure != MechanicalClawFailure.None) ObservedFailure = frame.Failure;
            if (frame.State == MechanicalClawState.Windup && frame.CastId != 0)
                _castIds.Add(frame.CastId);
            bool attached = frame.State is MechanicalClawState.Latched or MechanicalClawState.Reel or
                MechanicalClawState.Swing or MechanicalClawState.Boost;
            if (attached && frame.CastId != 0)
            {
                if (_latchIds.Add(frame.CastId)) LatchedTarget = target?.GetInstanceId() ?? 0;
                LatchedAnchor = frame.AnchorPoint;
            }
            HasAttached |= attached;
            if (SecondSpaceInputDispatched && frame.CastId == AttachedSecondSpaceCast)
            {
                bool leftHeld = Input.IsMouseButtonPressed(MouseButton.Left);
                BoostFromSecondSpaceObserved |= frame.State == MechanicalClawState.Boost;
                if (frame.State is MechanicalClawState.Released or MechanicalClawState.Retract)
                    ReleasedFromAttachedSecondSpace |= !leftHeld;
            }
        }

        public bool TryBeginAttachedSecondSpace(CharacterBody2D player, GrappleSurface2D expectedTarget)
        {
            MechanicalClawFrame frame = Claw.CurrentFrame;
            AttachedSecondSpaceCurrentState = frame.State is MechanicalClawState.Latched or
                MechanicalClawState.Reel or MechanicalClawState.Swing or MechanicalClawState.Boost;
            bool expectedCast = frame.CastId != 0 && frame.LockedTargetRuntimeId == ExpectedTarget &&
                Claw.CurrentTarget == expectedTarget;
            bool leftHeld = Input.IsMouseButtonPressed(MouseButton.Left);
            AttachedSecondSpaceAirborneAtEdge = !player.IsOnFloor();
            PrimaryReleasedBeforeSecondSpace = !leftHeld;
            if (!AttachedSecondSpaceCurrentState || !expectedCast || leftHeld ||
                !AttachedSecondSpaceAirborneAtEdge) return false;
            AttachedSecondSpaceCast = frame.CastId;
            return true;
        }

        public void RecordSecondSpaceInputDispatched() => SecondSpaceInputDispatched = true;

        public void CompleteAttachedSecondSpaceCausality()
        {
            AttachedSecondSpaceCausalityObserved = AttachedSecondSpaceCurrentState &&
                AttachedSecondSpaceAirborneAtEdge && AttachedSecondSpaceCast != 0 &&
                SecondSpaceInputDispatched && BoostFromSecondSpaceObserved &&
                PrimaryReleasedBeforeSecondSpace && ReleasedFromAttachedSecondSpace &&
                !SecondAirJumpAccepted;
        }

        public void Observe(CharacterBody2D player, TraversalSegment2D segment)
        {
            LeftFloor |= !player.IsOnFloor();
            StableLandingFrames = LeftFloor && player.IsOnFloor() &&
                HasLandingMargin(segment.LandingZone, player.Position) && HasExpectedLandingContact(player)
                ? StableLandingFrames + 1
                : 0;
        }

        private bool HasExpectedLandingContact(CharacterBody2D player)
        {
            if (ExpectedLandingSupport is not { } expected) return true;
            if (Mathf.Abs(player.Position.Y - expected.StandingY) > 1f ||
                player.Position.X < expected.Left || player.Position.X > expected.Right) return false;
            for (int index = 0; index < player.GetSlideCollisionCount(); index++)
            {
                // Borrowed native body cache wrapper; the reader does not own it.
                KinematicCollision2D contact = player.GetSlideCollision(index);
                if (contact.GetCollider() == expected.Body && contact.GetNormal().Y < -.99f) return true;
            }
            return false;
        }
    }
}
