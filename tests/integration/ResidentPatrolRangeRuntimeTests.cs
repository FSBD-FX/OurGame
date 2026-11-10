using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Knight.Combat;
using Knight.Enemies;
using Knight.Enemies.Residents;
using Knight.Player;
using Knight.Prologue;
using Knight.Prologue.Residents;
using Knight.World;
using Knight.Shared;
using Knight.Tests.Presentation;

namespace Knight.Tests.Integration;

// Controlled real saved bodies/Player on the actual saved bank, not manager/autospawn proof.
public static partial class ResidentPatrolRangeRuntimeTests
{
    public static async Task Run(SceneTree tree)
    {
        IsolatedPrologueFixture.ValidateStorageRoot();
        await RunCase(tree, "real-disengage", 900, true);
        await RunCase(tree, "far-authored", 1120, true);
        await RunCase(tree, "near-detection", 900, true);
        await RunCase(tree, "near-native-attack", 720, true);
        await RunCase(tree, "far-default-wave", 1120, false);
        GD.Print("RESIDENT_RANGE_NATIVE_OK cases=5 saved_actor=true saved_player=true authored_home=true " +
            "far_patrol=true near_detection=true near_attack=true real_disengage=true default_wave_unchanged=true isolated_fixture=true " +
            "normal_autospawn=false final_art=false human=false");
    }

    private static async Task RunCase(SceneTree tree, string name, float playerX, bool resident)
    {
        var fixture = new RangeFixture(tree);
        Exception failure = null, observationFailure = null;
        Action<RustShellFrame, int> sample = null;
        Action<AttackPresentationFrame> attack = null;
        Hitbox.ConnectedEventHandler connected = null;
        KeyboardRouteInputDriver driver = null;
        try
        {
            await fixture.Mount(playerX, resident);
            var actor = fixture.Actor;
            var player = fixture.Player;
            var hitbox = actor.GetNode<Hitbox>("AttackHitbox");
            int actorHp = fixture.ActorHealth.CurrentHealth, playerHp = fixture.PlayerHealth.CurrentHealth;
            int samples = 0, patrol = 0, engaged = 0, resumed = 0, returning = 0, active = 0, warnings = 0, connections = 0;
            float resumedMin = float.PositiveInfinity, resumedMax = float.NegativeInfinity;
            bool passedAuthoredHome = false;
            ulong lastTick = 0, warningId = 0, activeId = 0;
            float min = actor.GlobalPosition.X, max = min, previous = min, maxStep = 0;
            Vector2 playerStart = player.GlobalPosition;
            sample = (frame, _) =>
            {
                if (observationFailure != null) return;
                try
                {
                    ulong tick = Engine.GetPhysicsFrames();
                    TestAssert.True(Engine.IsInPhysicsFrame() && (lastTick == 0 || tick == lastTick + 1),
                        "RESIDENT_RANGE_GUARD: sample is each actual post-MoveAndSlide actor step");
                    lastTick = tick;
                    fixture.AssertNativeBodies();
                    TestAssert.True(actor.HasLineOfSightTo(player),
                        "RESIDENT_RANGE_GUARD: actual LOS must stay clear; occlusion is not a far-range counterexample");
                    float x = actor.GlobalPosition.X;
                    min = Mathf.Min(min, x); max = Mathf.Max(max, x);
                    maxStep = Mathf.Max(maxStep, Mathf.Abs(x - previous)); previous = x;
                    samples++;
                    if (resident && actor.ResidenceFrame.State == ResidentPatrolState.Patrolling)
                    {
                        patrol++;
                        if (engaged > 0)
                        {
                            resumed++; resumedMin = Mathf.Min(resumedMin, x); resumedMax = Mathf.Max(resumedMax, x);
                            passedAuthoredHome |= Mathf.Abs(x - 666) <= 2;
                        }
                    }
                    if (resident && actor.ResidenceFrame.State == ResidentPatrolState.Engaged) engaged++;
                    if (resident && actor.ResidenceFrame.State == ResidentPatrolState.Returning) returning++;
                    if (name.StartsWith("far-", StringComparison.Ordinal))
                    {
                        TestAssert.True(Mathf.Abs(player.GlobalPosition.X - x) > 360 &&
                            Mathf.Abs(player.GlobalPosition.X - playerStart.X) < .05f &&
                            fixture.ActorHealth.CurrentHealth == actorHp && fixture.PlayerHealth.CurrentHealth == playerHp,
                            "RESIDENT_RANGE_GUARD: real supported far Player stays outside360 with unchanged HP/pose");
                    }
                }
                catch (Exception error) { observationFailure ??= error; }
            };
            attack = frame =>
            {
                if (frame.Timeline.Phase == AttackPhase.Telegraph)
                { warnings++; warningId = frame.Timeline.AttackId; }
                if (frame.Timeline.Phase == AttackPhase.Active)
                { active++; activeId = frame.Timeline.AttackId; }
            };
            connected = hurtbox =>
            {
                if (ReferenceEquals(hurtbox, player.GetNode<Hurtbox>("Hurtbox"))) connections++;
            };
            actor.FrameChanged += sample;
            actor.AttackFrameChanged += attack;
            hitbox.Connected += connected;
            int cap = name == "near-native-attack" ? 180 : name == "near-detection" ? 60 : 120;
            if (name == "real-disengage")
            {
                TestAssert.True(actor.ResidenceFrame.State == ResidentPatrolState.Engaged,
                    "RESIDENT_DISENGAGE_GUARD: original resident begins genuinely engaged with the close real Player");
                driver = KeyboardRouteInputDriver.Begin(player);
                driver.Command(InputActions.MoveRight, true);
                for (int i = 0; i < 120 && player.GlobalPosition.X < 1180; i++)
                { await fixture.Frames(1); driver.ObserveFrame(); }
                driver.Command(InputActions.MoveRight, false);
                TestAssert.True(player.GlobalPosition.X >= 1180 && player.GlobalPosition.X < 1230,
                    "RESIDENT_DISENGAGE_GUARD: actual D input, not pose injection, must withdraw across the real bank");
                for (int i = 0; i < 180; i++)
                { await fixture.Frames(1); driver.ObserveFrame(); }
                driver.ReleaseAll(); driver.ObserveFrame();
                TestAssert.True(driver.KeyDownCount == 1 && driver.AttackEdges == 0 &&
                    driver.ClawEdges == 0 && driver.ShieldEdges == 0 && driver.ObservedFrames >= 180 &&
                    !Input.IsPhysicalKeyPressed(Key.D),
                    "RESIDENT_DISENGAGE_GUARD: only one ordinary D hold, no hidden combat/claw/guard intervention");
            }
            else
                for (int i = 0; i < cap && (name != "near-native-attack" || fixture.PlayerHealth.CurrentHealth == playerHp); i++)
                    await fixture.Frames(1);
            if (observationFailure != null) throw new InvalidOperationException("RESIDENT_RANGE_GUARD: observation failed", observationFailure);
            GD.Print($"RESIDENT_RANGE_TRACE case={name} samples={samples} min={min} max={max} max_step={maxStep} " +
                $"patrol={patrol} engaged={engaged} warning={warnings} active={active} connections={connections} " +
                $"warning_id={warningId} active_id={activeId} actor_hp={actorHp}->{fixture.ActorHealth.CurrentHealth} " +
                $"player_hp={playerHp}->{fixture.PlayerHealth.CurrentHealth} player={player.GlobalPosition} " +
                $"actor={actor.GlobalPosition} actor_id={actor.GetInstanceId()} player_id={player.GetInstanceId()} " +
                $"resumed={resumed} returning={returning} authored_home_passed={passedAuthoredHome} " +
                $"engine_keyboard={driver != null} after_tree_pose_velocity_hp_state_writes=0 normal_autospawn=false");
            TestAssert.True(samples >= (name == "near-native-attack" ? 10 : cap) && fixture.Recoveries == 0 &&
                fixture.ActorHealth.CurrentHealth == 3 && player.GetNode<PlayerMotor>("Motor").AcceptedJumpCount == 0 &&
                player.GetNode<PlayerMotor>("Motor").AcceptedDashCount == 0,
                "RESIDENT_RANGE_GUARD: nonvacuous native samples, no recovery/jump/thruster or hostile HP rewrite");
            if (name == "real-disengage")
                TestAssert.True(engaged > 0 && resumed >= 90 && returning == 0 && passedAuthoredHome &&
                    resumedMax - resumedMin > 40 && resumedMin < 664 &&
                    Mathf.Abs(player.GlobalPosition.X - actor.GlobalPosition.X) > 360 &&
                    fixture.PlayerHealth.CurrentHealth == playerHp && active == 0,
                    $"RESIDENT_DISENGAGE_PATROL_RED: real withdrawal must restore authored patrol and pass home without fabricated Returning; " +
                    $"engaged={engaged} resumed={resumed} home={passedAuthoredHome} range={(resumed > 0 ? resumedMax-resumedMin : 0)}");
            else if (name == "far-authored")
                TestAssert.True(patrol > 0 && engaged == 0 && max - min > 40 && maxStep < 2 && active == 0,
                    $"RESIDENT_FAR_PATROL_RED: clear real Player outside360 must not freeze authored patrol; " +
                    $"range={max-min} patrol={patrol} engaged={engaged} active={active}");
            else if (name == "far-default-wave")
                TestAssert.True(actor.CurrentState == RustShellState.Idle && max - min < .05f && active == 0,
                    "RESIDENT_RANGE_WAVE: unconfigured saved wave must not acquire autonomous patrol");
            else if (name == "near-detection")
                TestAssert.True(engaged > 0 && patrol == 0 && max - min > 40 && fixture.PlayerHealth.CurrentHealth == playerHp,
                    "RESIDENT_RANGE_NEAR: real target inside360 still attracts native chase rather than authored patrol");
            else
                TestAssert.True(engaged > 0 && warnings > 0 && active > 0 &&
                    warningId != 0 && warningId == activeId && connections > 0 && fixture.PlayerHealth.CurrentHealth < playerHp,
                    "RESIDENT_RANGE_NEAR: saved real Player still attracts chase/warning/active and native hitbox HP damage");
            GD.Print($"RESIDENT_RANGE_CASE_OK case={name} native=true engine_keyboard={driver != null} OS_origin=false");
        }
        catch (Exception error) { failure = error; }
        finally
        {
            await PrologueCanyonGeneratedMainRouteTests.Task10FinalizeRoute(failure, new Func<Task>[]
            {
                () => { driver?.Dispose(); return Task.CompletedTask; },
                () => { if (GodotObject.IsInstanceValid(fixture.Actor) && sample != null) fixture.Actor.FrameChanged -= sample; return Task.CompletedTask; },
                () => { if (GodotObject.IsInstanceValid(fixture.Actor) && attack != null) fixture.Actor.AttackFrameChanged -= attack; return Task.CompletedTask; },
                () => { if (GodotObject.IsInstanceValid(fixture.Actor) && connected != null) fixture.Actor.GetNode<Hitbox>("AttackHitbox").Connected -= connected; return Task.CompletedTask; },
                fixture.Dispose
            });
        }
    }

    private sealed class RangeFixture
    {
        private readonly SceneTree _tree;
        private World2D _world;
        private SubViewport _viewport;
        private RangeRecoveryScope _scope;
        private Node2D _donor;
        private StaticBody2D _floor;
        private ResidentPopulationDefinition _slot;
        public RustShellSquire Actor { get; private set; }
        public CharacterBody2D Player { get; private set; }
        public HealthComponent ActorHealth => Actor.GetNode<HealthComponent>("Health");
        public HealthComponent PlayerHealth => Player.GetNode<HealthComponent>("Health");
        public int Recoveries => _scope.Requests;
        public RangeFixture(SceneTree tree) { _tree = tree; }

        public async Task Mount(float playerX, bool resident, Action mounted = null)
        {
            using Godot.Collections.Array players = (Godot.Collections.Array)_tree.GetNodesInGroup("player");
            TestAssert.Equal(0, players.Count, "RESIDENT_RANGE_GUARD: no unrelated global Player target");
            _slot = ResidentAuthoredLayout.Load().Single(x =>
                x.Definition.StableId == "canyon.v2.resident.low-ceiling-bridge.01").Definition;
            TestAssert.True(_slot.Placement.WorldSpawn == new Vector2(666, 604) &&
                _slot.Placement.Territory == new Rect2(0, 0, 1280, 720) &&
                _slot.Placement.PatrolPoints.SequenceEqual(new[] { new Vector2(666, 604), new(602, 604), new(730, 604), new(666, 604) }),
                "RESIDENT_RANGE_GUARD: actual authored identity/home/territory/closed route, not replacement placement");
            _world = new World2D();
            _viewport = new SubViewport { Name = "SavedResidentRangeWorld", Size = new(1280, 720), World2D = _world };
            _scope = new RangeRecoveryScope();
            _viewport.AddChild(_scope);
            _donor = GD.Load<PackedScene>("res://scenes/prologue/sectors/CanyonEntrance.tscn").Instantiate<Node2D>();
            _floor = _donor.GetNode<StaticBody2D>("Gameplay/Collision/BridgeFarBank");
            _floor.GetParent().RemoveChild(_floor);
            _donor.Free(); _donor = null;
            // End the old scene Owner before reparenting its detached saved body.
            // This changes serialization ownership only, not any collision data.
            _scope.AddChild(_floor);
            if (resident)
            {
                var expected = ResidentSceneRegistry.Select(ResidentContentMode.DevelopmentV001)
                    .Single(x => x.Id == _slot.SubtypeId);
                Actor = (RustShellSquire)ResidentSceneBinding.Instantiate(GD.Load<PackedScene>(expected.ScenePath),
                    expected, _slot, allowPlaceholder: true);
            }
            else Actor = GD.Load<PackedScene>("res://scenes/enemies/RustShellSquire.tscn").Instantiate<RustShellSquire>();
            Actor.Position = _slot.Placement.WorldSpawn;
            Player = GD.Load<PackedScene>("res://scenes/player/Player.tscn").Instantiate<CharacterBody2D>();
            Player.Position = new(playerX, 600);
            Player.AddToGroup("player");
            _scope.AddChild(Actor); _scope.AddChild(Player);
            _tree.Root.AddChild(_viewport);
            mounted?.Invoke(); // Test observation only, before the original twelve settle frames.
            await Frames(12);
            var shape = _floor.GetNode<CollisionShape2D>("Collision");
            TestAssert.True(_floor.Position == new Vector2(840, 678) && _floor.CollisionLayer == 33 &&
                shape.Shape is RectangleShape2D rectangle && rectangle.Size == new Vector2(880, 84) &&
                _floor.GlobalTransform.IsEqualApprox(new Transform2D(0, new Vector2(840, 678))),
                "RESIDENT_RANGE_GUARD: actual unchanged saved bank transform/layer/shape top636");
            TestAssert.True(ActorHealth.CurrentHealth == 3 && PlayerHealth.CurrentHealth == 5 &&
                Player.GetWorld2D() == _world && _world != _tree.Root.FindWorld2D(),
                "RESIDENT_RANGE_GUARD: unchanged saved HP and independent shared native world");
            AssertNativeBodies();
        }

        public void AssertNativeBodies(bool targetMayBeAirborneAfterDamage = false)
        {
            TestAssert.True(Actor.IsOnFloor() && HasFloor(Actor) &&
                (targetMayBeAirborneAfterDamage || Player.IsOnFloor() && HasFloor(Player)) &&
                Actor.GetWorld2D() == _world && Player.GetWorld2D() == _world &&
                ResidentPatrolModel.Contains(_slot.Placement.Territory, Bounds(Actor)) &&
                ResidentPatrolModel.Contains(_slot.Placement.Territory, Bounds(Player)),
                "RESIDENT_RANGE_GUARD: both complete real bodies supported by the original bank and inside original territory");
        }
        private bool HasFloor(CharacterBody2D body)
        {
            for (int i = 0; i < body.GetSlideCollisionCount(); i++)
            {
                var contact = body.GetSlideCollision(i); // Borrowed, never disposed.
                if (contact.GetNormal().Dot(Vector2.Up) >= .7f && contact.GetCollider() == _floor) return true;
            }
            return false;
        }
        private static Rect2 Bounds(CharacterBody2D body)
        {
            var shape = body.GetNode<CollisionShape2D>("BodyCollider");
            return shape.GlobalTransform * shape.Shape.GetRect();
        }
        public async Task Frames(int count)
        {
            for (int i = 0; i < count; i++)
            { await _tree.ToSignal(_tree, SceneTree.SignalName.PhysicsFrame); await _tree.ToSignal(_tree, SceneTree.SignalName.ProcessFrame); }
        }
        public async Task Dispose()
        {
            await PrologueCanyonGeneratedMainRouteTests.Task10FinalizeRoute(null, new Func<Task>[]
            {
                async () => { if (GodotObject.IsInstanceValid(_viewport)) { if (_viewport.IsInsideTree()) await TestSceneTeardown.FreeSceneAfterAudioDrain(_tree, _viewport); else _viewport.Free(); } },
                () => { foreach (Node node in new Node[] { _donor, _scope, _floor, Actor, Player }) if (GodotObject.IsInstanceValid(node) && node.GetParent() == null) node.Free(); return Task.CompletedTask; },
                () =>
                {
                    TestAssert.True(!GodotObject.IsInstanceValid(_viewport) && !GodotObject.IsInstanceValid(_scope) &&
                        !GodotObject.IsInstanceValid(_donor) && !GodotObject.IsInstanceValid(_floor) &&
                        !GodotObject.IsInstanceValid(Actor) && !GodotObject.IsInstanceValid(Player),
                        "RESIDENT_RANGE_TEARDOWN: actual owned nodes must retire before the private World2D");
                    GD.Print("RESIDENT_RANGE_TEARDOWN_NODES_OK viewport_scope_floor_actor_player_retired=true borrowed_shapes_disposed=false");
                    return Task.CompletedTask;
                },
                () => { if (GodotObject.IsInstanceValid(_world)) _world.Dispose(); return Task.CompletedTask; }
            });
        }
    }

    private partial class RangeRecoveryScope : Node2D, ILevelRecovery
    {
        public int Requests { get; private set; }
        public void RequestRespawn(string reason) { Requests++; }
    }
}
