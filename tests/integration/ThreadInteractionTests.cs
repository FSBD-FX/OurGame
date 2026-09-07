using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using Knight.Abilities;
using Knight.Combat;
using Knight.Enemies;
using Knight.Enemies.Laborer;
using Knight.Enemies.Moth;
using Knight.Mechanisms;
using Knight.Player;
using Knight.Presentation;
using Knight.Vfx;

namespace Knight.Tests.Integration;

public static partial class ThreadInteractionTests
{
    private const float Frame = 1f / 60f;

    public static async Task Run(SceneTree tree)
    {
        await LightEnemyPullUsesAdapterWithoutPersistentRope(tree);
        await RustShellPullUsesRealTargetCollisionAndHonorsObstruction(tree);
        await HeavyEnemyExposureUsesAdapterWithoutPersistentRope(tree);
        await BellChainActivationUsesAdapterWithoutPersistentRope(tree);
        await DeadLegacyReceiverFailsWithoutLatchOrSideEffect(tree);
        await AdapterRejectedLegacyReceiverFailsWithoutLatchOrFallbackPull(tree);
        GD.Print("TASK6_FIX2_LEGACY_SEQUENCE_OK success=4 latched_each=1 released_each=1 " +
                 "failure_latched=0 presentation=ThreadHit");
    }

    private static async Task RustShellPullUsesRealTargetCollisionAndHonorsObstruction(SceneTree tree)
    {
        RustShellSquire squire = GD.Load<PackedScene>("res://scenes/enemies/RustShellSquire.tscn")
            .Instantiate<RustShellSquire>();
        squire.Name = "RustShellLightEnemy";
        squire.Position = new Vector2(220f, 0f);
        LegacyFixture fixture = await CreateFixture(tree, "RustShellAdapter", squire, "ThreadTarget");
        try
        {
            squire.SetPhysicsProcess(false);
            Area2D targetCollision = fixture.Target.GetNode<Area2D>("TargetCollision");
            TestAssert.Equal(CombatCollisionLayers.ThreadTarget, targetCollision.CollisionLayer,
                "generated Rust-Shell exposes a real layer-32 grapple target collider");
            TestAssert.True(targetCollision.GetNode<CollisionShape2D>("Collision").Shape != null,
                "generated Rust-Shell target collider has a real physics shape");

            List<MechanicalClawFrame> events = await CastLegacy(tree, fixture);
            TestAssert.True(squire.Velocity.X < 0f,
                "the real Rust-Shell is pulled toward the player through the legacy adapter");
            AssertAcceptedLegacySequence(fixture, events, "Rust-Shell light enemy pull");
        }
        finally { await Dispose(tree, fixture.Root); }

        RustShellSquire blockedSquire = GD.Load<PackedScene>("res://scenes/enemies/RustShellSquire.tscn")
            .Instantiate<RustShellSquire>();
        blockedSquire.Name = "BlockedRustShell";
        blockedSquire.Position = new Vector2(220f, 0f);
        LegacyFixture blocked = await CreateFixture(tree, "BlockedRustShellAdapter",
            blockedSquire, "ThreadTarget");
        try
        {
            blockedSquire.SetPhysicsProcess(false);
            blocked.Root.AddChild(WorldBlocker(new Vector2(110f, 0f), new Vector2(24f, 120f)));
            await Frames(tree, 2);
            Step(blocked, FrameInput(grappleModePressed: true, primaryPressed: true,
                primaryHeld: true, mouseWorld: blocked.Target.WorldAnchor));
            Step(blocked, FrameInput(primaryHeld: true, mouseWorld: blocked.Target.WorldAnchor));
            TestAssert.Equal(MechanicalClawFailure.Obstructed, blocked.Claw.CurrentFrame.Failure,
                "a real world collider blocks the Rust-Shell grapple query");
            TestAssert.Equal(0, blocked.Adapter.ApplicationCount,
                "obstruction prevents the Rust-Shell legacy pull side effect");
            Step(blocked, FrameInput(mouseWorld: blocked.Target.WorldAnchor));
            AssertNoPersistentRope(blocked, "obstructed Rust-Shell pull");
        }
        finally { await Dispose(tree, blocked.Root); }
    }

    private static async Task LightEnemyPullUsesAdapterWithoutPersistentRope(SceneTree tree)
    {
        GraveLampMoth moth = GD.Load<PackedScene>("res://scenes/enemies/GraveLampMoth.tscn")
            .Instantiate<GraveLampMoth>();
        moth.Name = "LightEnemy";
        moth.Position = new Vector2(220f, 0f);
        LegacyFixture fixture = await CreateFixture(tree, "LightEnemyAdapter", moth, "ThreadTarget",
            includePresentationProbe: true);
        try
        {
            List<MechanicalClawFrame> events = await CastLegacy(tree, fixture);
            TestAssert.Equal(PresentationCue.ThreadHit, fixture.PresentationProbe.LastCue,
                "the generated PlayerPresentationCoordinator consumes legacy Latched as ThreadHit");
            TestAssert.True(moth.Velocity.X < 0f,
                "the real light enemy is pulled toward the player through the legacy adapter");
            AssertAcceptedLegacySequence(fixture, events, "light enemy pull");
        }
        finally { await Dispose(tree, fixture.Root); }
    }

    private static async Task HeavyEnemyExposureUsesAdapterWithoutPersistentRope(SceneTree tree)
    {
        BellBackLaborer laborer = GD.Load<PackedScene>("res://scenes/enemies/BellBackLaborer.tscn")
            .Instantiate<BellBackLaborer>();
        laborer.Name = "HeavyEnemy";
        laborer.Position = new Vector2(240f, 0f);
        LegacyFixture fixture = await CreateFixture(tree, "HeavyEnemyAdapter", laborer, "ThreadTarget");
        try
        {
            laborer.SetPhysicsProcess(false);
            List<MechanicalClawFrame> events = await CastLegacy(tree, fixture);
            laborer._PhysicsProcess(0d);
            TestAssert.Equal(BellBackLaborerState.Exposed, laborer.CurrentState,
                "the real heavy enemy exposes its weak point through the legacy adapter");
            AssertAcceptedLegacySequence(fixture, events, "heavy enemy exposure");
        }
        finally { await Dispose(tree, fixture.Root); }
    }

    private static async Task BellChainActivationUsesAdapterWithoutPersistentRope(SceneTree tree)
    {
        BellChainMechanism mechanism = GD.Load<PackedScene>("res://scenes/mechanisms/BellChainMechanism.tscn")
            .Instantiate<BellChainMechanism>();
        mechanism.Name = "BellChain";
        mechanism.Position = new Vector2(260f, 80f);
        LegacyFixture fixture = await CreateFixture(tree, "MechanismAdapter", mechanism, "ThreadTarget");
        try
        {
            List<MechanicalClawFrame> events = await CastLegacy(tree, fixture);
            TestAssert.True(mechanism.IsOpen,
                "the real Bell Chain mechanism activates through the legacy adapter");
            AssertAcceptedLegacySequence(fixture, events, "Bell Chain activation");
        }
        finally { await Dispose(tree, fixture.Root); }
    }

    private static async Task DeadLegacyReceiverFailsWithoutLatchOrSideEffect(SceneTree tree)
    {
        GraveLampMoth moth = GD.Load<PackedScene>("res://scenes/enemies/GraveLampMoth.tscn")
            .Instantiate<GraveLampMoth>();
        moth.Name = "DeadLegacyReceiver";
        moth.Position = new Vector2(220f, 0f);
        LegacyFixture fixture = await CreateFixture(tree, "DeadLegacyAdapter", moth, "ThreadTarget");
        try
        {
            HealthComponent health = moth.GetNode<HealthComponent>("Health");
            health.ApplyDamage(new DamageRequest(health.MaxHealth, Vector2.Zero, 0f, 8801UL));
            Vector2 velocityBefore = moth.Velocity;
            List<MechanicalClawFrame> events = await DriveRejectedLegacyAttempt(tree, fixture);
            TestAssert.True(!events.Exists(frame => frame.State == MechanicalClawState.Latched),
                "a dead legacy receiver never publishes a successful Latched frame");
            TestAssert.Equal(1, events.FindAll(frame => frame.State == MechanicalClawState.Failed &&
                frame.Failure == MechanicalClawFailure.TargetLost).Count,
                "a dead locked receiver maps to one typed TargetLost failure");
            TestAssert.Equal(0, fixture.Adapter.ApplicationCount,
                "a dead legacy receiver has no adapter side effect");
            TestAssert.Equal(velocityBefore, moth.Velocity,
                "a dead light enemy receives no fallback pull velocity");
            AssertNoPersistentRope(fixture, "dead legacy receiver rejection");
        }
        finally { await Dispose(tree, fixture.Root); }
    }

    private static async Task AdapterRejectedLegacyReceiverFailsWithoutLatchOrFallbackPull(SceneTree tree)
    {
        RejectingLegacyReceiver receiver = RejectingReceiver(new Vector2(220f, 0f));
        LegacyFixture fixture = await CreateFixture(tree, "RejectedLegacyAdapter", receiver,
            "ThreadTarget");
        try
        {
            Vector2 playerVelocityBefore = fixture.Player.Velocity;
            List<MechanicalClawFrame> events = await DriveRejectedLegacyAttempt(tree, fixture);
            TestAssert.True(!events.Exists(frame => frame.State == MechanicalClawState.Latched),
                "an adapter-rejected legacy receiver never publishes Latched success");
            TestAssert.Equal(1, events.FindAll(frame => frame.State == MechanicalClawState.Failed &&
                frame.Failure == MechanicalClawFailure.TargetLost).Count,
                "adapter rejection maps to one typed TargetLost failure");
            TestAssert.Equal(1, receiver.AttemptCount,
                "the rejecting receiver is consulted exactly once");
            TestAssert.Equal(0, fixture.Adapter.ApplicationCount,
                "adapter rejection is not counted as an applied interaction");
            TestAssert.Equal(playerVelocityBefore, fixture.Player.Velocity,
                "adapter rejection cannot fall back to pulling the player");
            AssertNoPersistentRope(fixture, "adapter-rejected legacy receiver");
        }
        finally { await Dispose(tree, fixture.Root); }
    }

    private static async Task<LegacyFixture> CreateFixture(SceneTree tree, string name,
        Node2D receiver, NodePath targetPath, bool includePresentationProbe = false)
    {
        var root = new Node2D { Name = name };
        root.AddChild(receiver);
        PrologueVfxDirector presentationProbe = null;
        if (includePresentationProbe)
        {
            presentationProbe = new PrologueVfxDirector { Name = "PresentationProbe" };
            root.AddChild(presentationProbe);
        }
        CharacterBody2D player = GD.Load<PackedScene>("res://scenes/player/Player.tscn")
            .Instantiate<CharacterBody2D>();
        player.GetNode<Camera2D>("Camera2D").Enabled = false;
        root.AddChild(player);
        tree.Root.AddChild(root);
        await Frames(tree, 2);

        ThreadTarget2D target = receiver.GetNode<ThreadTarget2D>(targetPath);
        LegacyThreadInteractionAdapter adapter = target.GetNode<LegacyThreadInteractionAdapter>(
            "LegacyThreadInteractionAdapter");
        PlayerInputReader input = player.GetNode<PlayerInputReader>("Input");
        PlayerActionModeController modes = player.GetNode<PlayerActionModeController>("ActionModes");
        MechanicalClawController claw = player.GetNode<MechanicalClawController>("MechanicalClaw");
        ThreadLineVisual line = player.GetNode<ThreadLineVisual>("VisualRoot/MechanicalClaw/Line");
        input.SetPhysicsProcess(false);
        modes.SetPhysicsProcess(false);
        claw.SetPhysicsProcess(false);
        player.GetNode<PlayerMotor>("Motor").SetPhysicsProcess(false);
        player.GetNode<PlayerShieldController>("Shield").SetPhysicsProcess(false);
        player.GetNode<PlayerEnergyController>("Energy").SetPhysicsProcess(false);
        player.GetNode<PlayerCombat>("Combat").SetPhysicsProcess(false);
        return new LegacyFixture(root, player, receiver, target, adapter, input, modes, claw, line,
            player.GetNode<PlayerMotor>("Motor"), presentationProbe);
    }

    private static async Task<List<MechanicalClawFrame>> CastLegacy(SceneTree tree, LegacyFixture fixture)
    {
        var events = new List<MechanicalClawFrame>();
        fixture.Claw.ClawFrameChanged += Record;
        try
        {
            fixture.Claw.ResetTransientState();
            Step(fixture, FrameInput(grappleModePressed: true, primaryPressed: true,
                primaryHeld: true, mouseWorld: fixture.Target.WorldAnchor));
            for (int index = 0; index < 120 && fixture.Adapter.ApplicationCount == 0; index++)
            {
                Step(fixture, FrameInput(primaryHeld: true, mouseWorld: fixture.Target.WorldAnchor));
                await Frames(tree, 1);
            }
            TestAssert.Equal(1, fixture.Adapter.ApplicationCount,
                "one accepted legacy target receives exactly one adapter application per claw cast");
            return events;
        }
        finally { fixture.Claw.ClawFrameChanged -= Record; }

        void Record(MechanicalClawFrame frame, GrappleSurface2D target) => events.Add(frame);
    }

    private static async Task<List<MechanicalClawFrame>> DriveRejectedLegacyAttempt(SceneTree tree,
        LegacyFixture fixture)
    {
        var events = new List<MechanicalClawFrame>();
        fixture.Claw.ClawFrameChanged += Record;
        fixture.Claw.ResetTransientState();
        Step(fixture, FrameInput(grappleModePressed: true, primaryPressed: true,
            primaryHeld: true, mouseWorld: fixture.Target.WorldAnchor));
        for (int index = 0; index < 120 &&
            !events.Exists(frame => frame.State == MechanicalClawState.Failed); index++)
        {
            Step(fixture, FrameInput(primaryHeld: true, mouseWorld: fixture.Target.WorldAnchor));
            await Frames(tree, 1);
        }
        fixture.Claw.ClawFrameChanged -= Record;
        return events;

        void Record(MechanicalClawFrame frame, GrappleSurface2D target) => events.Add(frame);
    }

    private static void AssertNoPersistentRope(LegacyFixture fixture, string context)
    {
        TestAssert.True(fixture.Claw.CurrentFrame.State is not MechanicalClawState.Latched and
                not MechanicalClawState.Reel and not MechanicalClawState.Swing and
                not MechanicalClawState.Boost,
            $"{context} immediately leaves every persistent attached state, including Boost");
        TestAssert.True(fixture.Line.CurrentState == ThreadLineState.Idle,
            $"{context} leaves no persistent world rope line");
        TestAssert.True(fixture.Claw.CurrentTarget == null,
            $"{context} releases the public current target after applying the adapter");
    }

    private static void AssertAcceptedLegacySequence(LegacyFixture fixture,
        List<MechanicalClawFrame> events, string context)
    {
        int latchedIndex = events.FindIndex(frame => frame.State == MechanicalClawState.Latched);
        int releasedIndex = events.FindIndex(frame => frame.State == MechanicalClawState.Released);
        TestAssert.Equal(1, events.FindAll(frame => frame.State == MechanicalClawState.Latched).Count,
            $"{context} publishes exactly one real Latched edge after its accepted side effect");
        TestAssert.Equal(1, events.FindAll(frame => frame.State == MechanicalClawState.Released).Count,
            $"{context} publishes exactly one immediate Released edge");
        TestAssert.True(latchedIndex >= 0 && releasedIndex > latchedIndex,
            $"{context} orders accepted legacy frames as Latched then Released");
        TestAssert.Equal(1, fixture.Adapter.ApplicationCount,
            $"{context} applies its business side effect exactly once");
        TestAssert.Equal(MechanicalClawState.Released, fixture.Claw.CurrentFrame.State,
            $"{context} finishes on Released instead of retaining a latch");
        TestAssert.Equal(Vector2.Zero, fixture.Claw.CurrentFrame.VelocityCorrection,
            $"{context} leaves no model velocity correction after release");
        TestAssert.Near(1f, fixture.Motor.GrappleMovementScale, .0001f,
            $"{context} leaves no motor grapple movement modifier after release");
        AssertNoPersistentRope(fixture, context);
    }

    private static void Step(LegacyFixture fixture, PlayerInputFrame frame)
    {
        SetInputFrame(fixture.Input, frame);
        fixture.Modes._PhysicsProcess(Frame);
        fixture.Claw._PhysicsProcess(Frame);
    }

    private static PlayerInputFrame FrameInput(bool grappleModePressed = false,
        bool primaryPressed = false, bool primaryHeld = false, Vector2 mouseWorld = default) =>
        new(0f, Vector2.Zero, false, false, false, false, false,
            MouseWorld: mouseWorld,
            GrappleModePressed: grappleModePressed,
            PrimaryPressed: primaryPressed,
            PrimaryHeld: primaryHeld);

    private static void SetInputFrame(PlayerInputReader input, PlayerInputFrame frame) =>
        typeof(PlayerInputReader).GetProperty(nameof(PlayerInputReader.CurrentFrame),
            BindingFlags.Instance | BindingFlags.Public)!.SetValue(input, frame);

    private static async Task Dispose(SceneTree tree, Node root)
    {
        root.QueueFree();
        await Frames(tree, 2);
    }

    private static async Task Frames(SceneTree tree, int count)
    {
        for (int index = 0; index < count; index++)
            await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
    }

    private static StaticBody2D WorldBlocker(Vector2 position, Vector2 size)
    {
        var blocker = new StaticBody2D
        {
            Name = "WorldBlocker",
            Position = position,
            CollisionLayer = CombatCollisionLayers.World,
            CollisionMask = CombatCollisionLayers.None
        };
        blocker.AddChild(new CollisionShape2D
        {
            Name = "Collision",
            Shape = new RectangleShape2D { Size = size }
        });
        return blocker;
    }

    private static RejectingLegacyReceiver RejectingReceiver(Vector2 position)
    {
        var receiver = new RejectingLegacyReceiver { Name = "RejectingReceiver", Position = position };
        var target = new ThreadTarget2D
        {
            Name = "ThreadTarget",
            Kind = ThreadTargetKind.HeavyEnemy,
            Material = GrappleSurfaceMaterial.TempleMetal,
            PullReceiverPath = new NodePath("..")
        };
        var targetCollision = new Area2D
        {
            Name = "TargetCollision",
            CollisionLayer = CombatCollisionLayers.ThreadTarget,
            CollisionMask = CombatCollisionLayers.None,
            Monitoring = true,
            Monitorable = true
        };
        targetCollision.AddChild(new CollisionShape2D
        {
            Name = "Collision",
            Shape = new CircleShape2D { Radius = 18f }
        });
        target.AddChild(targetCollision);
        target.AddChild(new LegacyThreadInteractionAdapter
        {
            Name = "LegacyThreadInteractionAdapter",
            TargetPath = new NodePath("..")
        });
        receiver.AddChild(target);
        return receiver;
    }

    private sealed record LegacyFixture(
        Node2D Root,
        CharacterBody2D Player,
        Node2D Receiver,
        ThreadTarget2D Target,
        LegacyThreadInteractionAdapter Adapter,
        PlayerInputReader Input,
        PlayerActionModeController Modes,
        MechanicalClawController Claw,
        ThreadLineVisual Line,
        PlayerMotor Motor,
        PrologueVfxDirector PresentationProbe);

    private sealed partial class RejectingLegacyReceiver : Node2D, IThreadInteractionReceiver
    {
        public int AttemptCount { get; private set; }

        public bool TryReceiveThread(ThreadInteraction interaction)
        {
            AttemptCount++;
            return false;
        }
    }
}
