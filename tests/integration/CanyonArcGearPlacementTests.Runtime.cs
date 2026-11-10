using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using Knight.Combat;
using Knight.Mechanisms;
using Knight.Player;
using Knight.Presentation.Fog;
using Knight.Prologue;
using Knight.Prologue.Residents;
using Knight.Prologue.Seamless;
using Knight.Shared;
using Knight.Tests.Presentation;

namespace Knight.Tests.Integration;

public static partial class CanyonArcGearPlacementTests
{
    private static async Task RunRuntime(SceneTree tree, string parentPath)
    {
        for (int variant = 0; variant < 4; variant++)
        {
            bool rightStart = variant % 2 != 0;
            var f = new ArcGalleryFixture(tree, parentPath, variant, rightStart ? 3550 : 3250);
            try
            {
                await f.Start();
                if (variant == 0) await f.CheckFog();
                await f.WaitPhase(variant * .25);
                double start = f.Gear.CurrentMotion.Phase01;
                await f.Walk(rightStart ? 3250 : 3550); f.AssertSafe();
                await f.Walk(rightStart ? 3550 : 3250); f.AssertSafe();
                f.AssertSupportAndCamera();
                TestAssert.True(f.Crossings.Count == 2 && f.Crossings.All(p => double.IsFinite(p.Phase)),
                    "both real passages record their actual center-crossing phase, not only the starting phase");
                GD.Print($"ARC_GEAR_WALK_PHASE variant={variant} start={start:R} outward={f.Crossings[0].Phase:R} return={f.Crossings[1].Phase:R} direction={(rightStart ? "left" : "right")} hp={f.Health.CurrentHealth} native_support=true");
            }
            finally { tree.Paused = false; await f.Dispose(); }
        }
        GD.Print("ARC_GEAR_SAFE_PASSAGE_OK start_phases=4 trips=8 real_keyboard=true actual_crossings=true actual_support=true full_capsule_camera=true damage=0 recoveries=0 jump=0 dash=0 claw=0 controlled_start=true combat_isolated=true natural_full_route=false human=false");

        for (int variant = 0; variant < 3; variant++)
        {
            var f = new ArcGalleryFixture(tree, parentPath, 10 + variant, 3400);
            try
            {
                await f.Start(); await f.WaitPhase(variant == 1 ? .65 : .15);
                bool shortTap = variant == 2;
                ulong before = f.Motor.AcceptedJumpSequence;
                f.Keyboard.Command(InputActions.Jump, true); await f.Frame();
                if (shortTap) f.Keyboard.Command(InputActions.Jump, false);
                bool airborne = !f.Player.IsOnFloor(), landed = false;
                for (int frame = 0; frame < 100; frame++)
                {
                    if (!shortTap && frame == 24) f.Keyboard.Command(InputActions.Jump, false);
                    await f.Frame(); airborne |= !f.Player.IsOnFloor();
                    if (airborne && f.Player.IsOnFloor()) { landed = true; break; }
                }
                f.Keyboard.ReleaseAll(); await f.Frames(4);
                TestAssert.True(landed && f.Motor.AcceptedJumpSequence == before + 1,
                    "exactly one real ordinary ground Space returns to the unchanged native walkway");
                f.AssertSupportAndCamera();
                if (shortTap) f.AssertSafe();
                else TestAssert.True(f.Hits.Count == 1 && f.Hits[0].SourceRuntimeId == f.Gear.GetInstanceId() &&
                    f.Hits[0].LocalAttackId > 0 && f.ContactIds.Contains(f.Hits[0].LocalAttackId) &&
                    f.Health.CurrentHealth == f.Health.MaxHealth - 2 && f.ObserverFailure == null,
                    "ARC_GEAR_HIGH_JUMP_RED: held Space must meet the actual arc once for configured2 accepted damage: " + f.Diagnostics());
                GD.Print($"ARC_GEAR_JUMP_CASE variant={variant} short_tap={shortTap} accepted=1 hits={f.Hits.Count} hp={f.Health.CurrentHealth} source={(f.Hits.Count == 0 ? 0 : f.Hits[0].SourceRuntimeId)} arc={f.Gear.GetInstanceId()} recoveries=0 native_support=true");
            }
            finally { await f.Dispose(); }
        }
        GD.Print("ARC_GEAR_HIGH_JUMP_OK directions=2 accepted_hits_each=1 damage_each=2 source=saved_arc contact_identity=true short_tap_safe=true ordinary_jump_each=1 recovery=0 real_keyboard=true controlled_start=true combat_isolated=true natural_full_route=false human=false");
    }

    private sealed class ArcGalleryFixture
    {
        private readonly SceneTree _tree;
        internal readonly PrologueLevelController Level;
        internal readonly CharacterBody2D Player;
        internal readonly PlayerMotor Motor;
        internal readonly HealthComponent Health;
        internal readonly SeamlessPrologueWorld World;
        internal readonly KeyboardRouteInputDriver Keyboard;
        internal CanyonArcGear Gear;
        internal readonly List<DamageAttackIdentity> Hits = new();
        internal readonly List<ulong> ContactIds = new();
        internal readonly List<(float X, double Phase)> Crossings = new();
        internal string ObserverFailure;
        private readonly ulong _playerId;
        private readonly int _variant;
        private int _recoveries, _recoveriesCompleted;
        private StaticBody2D _support;
        private readonly List<(Node Node, string Description)> _disabled = new();
        private readonly List<CanyonSteamJet> _isolatedJets = new();
        private bool _jetIsolationActive;
        private readonly Queue<string> _recent = new();

        internal ArcGalleryFixture(SceneTree tree, string path, int variant, float x)
        {
            _tree = tree; _variant = variant;
            Level = GD.Load<PackedScene>(path).Instantiate<PrologueLevelController>();
            Level.ResidentContent = ResidentContentMode.FinalOnly; // Explicit controlled-fixture content override.
            Player = Level.GetNode<CharacterBody2D>("PlayerRoot/Player");
            Player.Position = new(x, 4080); // Only Player pose write, before AddChild.
            _playerId = Player.GetInstanceId();
            Motor = Player.GetNode<PlayerMotor>("Motor"); Health = Player.GetNode<HealthComponent>("Health");
            World = Level.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors");
            World.InitialSector = PrologueSectorId.CanyonBasin;
            var flow = Level.GetNode<PrologueGameFlowController>("GameFlow");
            flow.HeadlessAutoStart = false; flow.AutomationAutoStart = true;
            IsolatedPrologueFixture.Configure(Level, "under-arc-" + variant);
            Keyboard = KeyboardRouteInputDriver.Begin(Player);
            World.SectorMounted += IsolateMountedCombat;
            Health.Damaged += OnDamaged; Level.RecoveryRequested += OnRecovery;
            Level.RecoveryCompleted += OnRecovered;
        }

        // SectorMounted runs after staged collisions are restored but before the
        // first physics step. No world terrain or other mechanism is disabled.
        private void IsolateMountedCombat(PrologueSector sector)
        {
            if (sector.SectorId == PrologueSectorId.CanyonBasin)
            {
                // Actual SteamJet shares this lane; normal Explore closes it
                // through W+K at the existing valve before reaching the arc.
                // This controlled gear fixture isolates that one other source,
                // not a claim of safe passage while the steam valve is unopened.
                var jet = sector.GetNode<CanyonSteamJet>("Gameplay/Mechanisms/SteamJet");
                GD.Print($"ARC_GEAR_FIXTURE_OTHER_SOURCE path={jet.GetPath()} type={jet.GetType().Name} id={jet.GetInstanceId()} monitoring={jet.Monitoring} disabled={jet.DisabledForRun} prerequisite=original_valve_closed controlled_isolation=true");
                _isolatedJets.Add(jet);
            }
            foreach (Node node in CanyonArcGearPlacementTests.Walk(sector.GetNode("Gameplay/Encounters")))
                if (node is CanyonEncounterGroup)
                {
                    _disabled.Add((node, $"{node.GetPath()}|{node.GetType().Name}|id={node.GetInstanceId()}|process={node.ProcessMode}|physics={node.IsPhysicsProcessing()}"));
                    node.ProcessMode = Node.ProcessModeEnum.Disabled; node.SetProcess(false); node.SetPhysicsProcess(false);
                }
            foreach (Node node in CanyonArcGearPlacementTests.Walk(sector.GetNode("Gameplay/Actors")))
            {
                _disabled.Add((node, $"{node.GetPath()}|{node.GetType().Name}|id={node.GetInstanceId()}|process={node.ProcessMode}"));
                node.ProcessMode = Node.ProcessModeEnum.Disabled; node.SetProcess(false); node.SetPhysicsProcess(false);
                if (node is CollisionObject2D body)
                {
                    _disabled.Add((node, $"{node.GetPath()}|layer={body.CollisionLayer}|mask={body.CollisionMask}"));
                    body.CollisionLayer = 0; body.CollisionMask = 0;
                }
                if (node is Area2D area) { area.Monitoring = false; area.Monitorable = false; }
                if (node is CollisionShape2D shape) shape.Disabled = true;
            }
        }

        internal async Task Start()
        {
            _tree.Root.AddChild(Level); await Frames(8);
            await BasinAscentCameraRuntimeTests.WaitForPlaying(_tree, Level);
            // Level's later SectorMounted listener reapplies durable controls
            // and legitimately enables an unopened valve's jet. Isolate only
            // after that real session has committed, before any test traversal.
            // Any damage during startup is still rejected by AssertSafe below.
            foreach (var jet in _isolatedJets) jet.DisableForRun();
            _jetIsolationActive = true;
            TestAssert.True(World.TryGetMountedSector(PrologueSectorId.CanyonBasin, out var basin), "actual Basin stream mounted");
            Gear = basin.GetNode<CanyonArcGear>(GearPath);
            Gear.Contacted += OnContact;
            TestAssert.True(Gear.HasValidMotionGeometry && Gear.IsDangerTelegraphActive &&
                (Gear.GlobalPosition - Gear.CurrentMotion.Offset + Gear.PivotOffset).DistanceTo(new(3400, 3716)) < .01f,
                "saved staged arc is active and actually attached to the unmodified gallery underside");
            var reservation = ResidentEnvironmentInventory.Capture(World).Regions.Single(r => r.SourceId.StartsWith("arc-gear:", StringComparison.Ordinal));
            TestAssert.True(reservation.WorldBounds.Position.DistanceTo(new(3296, 3932)) < .01f &&
                reservation.WorldBounds.End.DistanceTo(new(3504, 3992.9822f)) < .01f,
                "mounted inventory reserves the complete future arc, not the endpoint chord or current circle");
            TestAssert.True(ResidentAuthoredLayout.Load().All(s => !s.Occupancy.Intersects(reservation.WorldBounds)),
                "complete arc keeps all50 authored occupancy boxes; this is not native combat proof");
            _support = BasinAscentCameraRuntimeTests.ResolveAuthoredSupport(World, PrologueSectorId.CanyonBasin,
                "ShortcutBasinWalkway", new(2080, 4116, 1720, 28));
            await Until(() => Player.IsOnFloor() && Mathf.Abs(Player.Velocity.X) < 1, 90, "settle naturally on walkway");
            AssertSupportAndCamera(); AssertSafe();
            TestAssert.True(Level.ResidentContent == ResidentContentMode.FinalOnly && !Level.NeedsResidentPreparation &&
                _disabled.Count > 0, "declared controlled combat isolation is actually applied");
            foreach (var record in _disabled) GD.Print("ARC_GEAR_FIXTURE_DISABLED " + record.Description);
            TestAssert.True(_isolatedJets.Count == 1 && _isolatedJets[0].DisabledForRun,
                "original paired steam is explicitly isolated; natural route must still really close the valve");
            GD.Print($"ARC_GEAR_FIXTURE_ISOLATION variant={_variant} resident_mode=FinalOnly records={_disabled.Count} isolated_paired_steam=1 steam_after_playing=true post_staged_mount=true other_mechanisms_retained=true terrain_player_input_camera_fog_retained=true normal_route=false");
        }

        internal async Task Frame()
        {
            await _tree.ToSignal(_tree, SceneTree.SignalName.PhysicsFrame);
            await _tree.ToSignal(_tree, SceneTree.SignalName.ProcessFrame);
            Keyboard.ObserveFrame();
            _recent.Enqueue($"p={Player.GlobalPosition} v={Player.Velocity} hp={Health.CurrentHealth} floor={Player.IsOnFloor()} phase={(GodotObject.IsInstanceValid(Gear) ? Gear.CurrentMotion.Phase01 : -1)} hits={Hits.Count}");
            if (_recent.Count > 12) _recent.Dequeue();
            TestAssert.True(Player.GetInstanceId() == _playerId && Level.Player == Player && Health.CurrentHealth > 0 &&
                _recoveries == 0 && _recoveriesCompleted == 0 && ObserverFailure == null &&
                Player.GlobalPosition.IsFinite() && Player.GlobalPosition.Y is > 3850 and < 4120 &&
                Motor.AcceptedDashCount == 0 && Keyboard.ClawEdges == 0 &&
                World.CurrentSector == PrologueSectorId.CanyonBasin, "no hidden relocation/death/dash/claw/recovery: " + Diagnostics());
            foreach (var pair in _disabled)
                if (GodotObject.IsInstanceValid(pair.Node))
                    TestAssert.True(pair.Node.ProcessMode == Node.ProcessModeEnum.Disabled &&
                        (pair.Node is not CollisionObject2D body || body.CollisionLayer == 0 && body.CollisionMask == 0) &&
                        (pair.Node is not Area2D area || !area.Monitoring && !area.Monitorable) &&
                        (pair.Node is not CollisionShape2D c || c.Disabled), "combat isolation cannot silently reactivate: " + pair.Description);
            foreach (var jet in _jetIsolationActive ? _isolatedJets : Enumerable.Empty<CanyonSteamJet>())
                TestAssert.True(!GodotObject.IsInstanceValid(jet) || jet.DisabledForRun && !jet.Monitoring && jet.Collision.Disabled,
                    "the single declared other-source isolation cannot silently reactivate");
            foreach (PrologueSector sector in World.GetChildren().OfType<PrologueSector>())
                TestAssert.True(CanyonArcGearPlacementTests.Walk(sector.GetNode("Gameplay/Actors")).Count(n => n is CharacterBody2D) == 0,
                    "no enemy may spawn after controlled combat isolation");
        }
        internal async Task Frames(int n) { for (int i = 0; i < n; i++) await Frame(); }
        private async Task Until(Func<bool> condition, int cap, string reason)
        { for (int i = 0; i < cap && !condition(); i++) await Frame(); TestAssert.True(condition(), reason + ": " + Diagnostics()); }
        internal async Task WaitPhase(double phase) => await Until(() =>
            Gear.CurrentMotion.Phase01 >= phase && Gear.CurrentMotion.Phase01 <= phase + .02, 240, "natural start phase " + phase);
        internal async Task Walk(float target)
        {
            TestAssert.True(Motor.AcceptedJumpSequence == 0, "walking matrix cannot borrow any jump");
            float previous = Player.GlobalPosition.X;
            for (int frame = 0; frame < 180; frame++)
            {
                float projected = Player.GlobalPosition.X + Player.Velocity.X * Mathf.Abs(Player.Velocity.X) / (2 * 2800f);
                Keyboard.Command(InputActions.MoveRight, projected < target - 2);
                Keyboard.Command(InputActions.MoveLeft, projected > target + 2);
                await Frame(); AssertSafe();
                float now = Player.GlobalPosition.X;
                if (previous < 3400 && now >= 3400 || previous > 3400 && now <= 3400)
                    Crossings.Add((now, Gear.CurrentMotion.Phase01));
                previous = now;
                if (Mathf.Abs(now - target) < 5 && Mathf.Abs(Player.Velocity.X) < 15)
                { Keyboard.ReleaseAll(); await Frames(3); return; }
            }
            Keyboard.ReleaseAll(); TestAssert.True(false, "bounded natural walk cannot reach " + target + ": " + Diagnostics());
        }
        internal void AssertSafe() => TestAssert.True(Health.CurrentHealth == Health.MaxHealth && Hits.Count == 0 &&
            _recoveries == 0 && _recoveriesCompleted == 0 && ObserverFailure == null, "safe passage cannot spend HP: " + Diagnostics());
        internal void AssertSupportAndCamera()
        {
            bool support = false;
            for (int i = 0; i < Player.GetSlideCollisionCount(); i++)
            { using var collision = Player.GetSlideCollision(i); support |= collision.GetCollider() == _support && collision.GetNormal().Y < -.7f; }
            TestAssert.True(Player.IsOnFloor() && support && Player.GlobalPosition.X is > 3200 and < 3600,
                "full player is supported by original walkway, not recovery or a new catch shelf: " + Diagnostics());
            var body = Player.GetNode<CollisionShape2D>("BodyCollider");
            TestAssert.True(body.Shape is CapsuleShape2D { Radius: 20, Height: 72 }, "unchanged real40x72 capsule");
            Transform2D canvas = body.GetGlobalTransformWithCanvas(); Vector2 size = Player.GetViewportRect().Size;
            foreach (Vector2 corner in new[] { new Vector2(-20, -36), new(20, -36), new(20, 36), new(-20, 36) })
            {
                Vector2 point = canvas * corner;
                TestAssert.True(point.X >= 16 && point.Y >= 16 && point.X <= size.X - 16 && point.Y <= size.Y - 16,
                    "unchanged camera retains complete capsule with16px margin");
            }
            var camera = Player.GetNode<Camera2D>("Camera2D");
            TestAssert.True(camera.Enabled && camera.LimitLeft == 0 && camera.LimitTop == 0 &&
                camera.LimitRight == 7680 && camera.LimitBottom == 4320, "original camera bounds unchanged");
        }
        internal async Task CheckFog()
        {
            var fog = Level.GetNode<PrologueFogController>("FogController");
            var overlay = Level.GetNode<FogRecyclingReadabilityOverlay>("ProtectedCues/Recycling");
            void Submit() => RenderingServer.Singleton.EmitSignal(RenderingServer.SignalName.FramePreDraw);
            Submit(); int baseline = overlay.PresentedGearCount;
            TestAssert.True(baseline >= 1 && fog.LastOcclusionFrame is { IsComplete: true }, "saved fog exposes actual nearby arc circle");
            Gear.Visible = false; Submit(); TestAssert.Equal(baseline - 1, overlay.PresentedGearCount, "hidden arc retracts exactly its own commands");
            Gear.Visible = true; Submit(); TestAssert.Equal(baseline, overlay.PresentedGearCount, "arc command restored without revealing its full future path");
            using var wallShape = new RectangleShape2D { Size = new(8, 250) };
            var wall = new StaticBody2D { Name = "ArcCurrentCircleFogWall", Position = new(3285, 3980), CollisionLayer = CombatCollisionLayers.World };
            wall.AddChild(new CollisionShape2D { Shape = wallShape }); Level.GetNode<Node2D>("WorldRoot").AddChild(wall);
            try { await Frames(2); Submit(); TestAssert.Equal(baseline - 1, overlay.PresentedGearCount, "real wall blocks current arc circle, not authored-origin proxy"); }
            finally { wall.QueueFree(); await Frames(2); }
            Submit(); TestAssert.Equal(baseline, overlay.PresentedGearCount, "wall removal restores actual circle");
            _tree.Paused = true;
            try { var motion = Gear.CurrentMotion; Submit(); TestAssert.True(overlay.PresentedSegmentCount == 0 && Gear.CurrentMotion == motion, "pause retracts commands without motion"); }
            finally { _tree.Paused = false; }
            Submit(); TestAssert.Equal(baseline, overlay.PresentedGearCount, "resume restores same saved-controller source");
            GD.Print("ARC_GEAR_SAVED_CONTROLLER_OK actual_saved_parent=true staged_ready=true future_reservation=true current_circle=true real_wall=true hidden_pause=true manual_predraw=true pixels=false human=false");
        }
        internal string Diagnostics() => string.Join("; ", _recent) + " observer=" + ObserverFailure;
        private void OnContact(Vector2 _, ulong identity) => ContactIds.Add(identity);
        private void OnRecovery(string _) => _recoveries++;
        private void OnRecovered(string _) => _recoveriesCompleted++;
        private void OnDamaged(int _, Vector2 __)
        {
            try
            {
                var field = typeof(HealthComponent).GetField("_processedAttackOrder", BindingFlags.NonPublic | BindingFlags.Instance);
                Hits.Add(((Queue<DamageAttackIdentity>)field.GetValue(Health)).Last());
                if (Hits.Count <= 16)
                {
                    DamageAttackIdentity hit = Hits[^1];
                    GodotObject source = GodotObject.InstanceFromId(hit.SourceRuntimeId);
                    GD.Print($"ARC_GEAR_ACCEPTED_DAMAGE variant={_variant} health_signal_value={_} hp={Health.CurrentHealth} source={hit.SourceRuntimeId} attack={hit.LocalAttackId} type={source?.GetType().FullName} path={(source is Node n && n.IsInsideTree() ? n.GetPath().ToString() : "<no-live-path>")} player={Player.GlobalPosition} arc={Gear?.GetInstanceId()} arc_position={Gear?.GlobalPosition} phase={Gear?.CurrentMotion.Phase01:R}");
                }
            }
            catch (Exception error) { ObserverFailure = error.ToString(); }
        }
        internal async Task Dispose()
        {
            try
            {
                Keyboard.Dispose(); World.SectorMounted -= IsolateMountedCombat;
                Health.Damaged -= OnDamaged; Level.RecoveryRequested -= OnRecovery; Level.RecoveryCompleted -= OnRecovered;
                if (GodotObject.IsInstanceValid(Gear)) Gear.Contacted -= OnContact;
            }
            finally
            {
                if (GodotObject.IsInstanceValid(Level))
                { if (Level.IsInsideTree()) await TestSceneTeardown.FreeSceneAfterAudioDrain(_tree, Level); else Level.Free(); }
            }
        }
    }
}
