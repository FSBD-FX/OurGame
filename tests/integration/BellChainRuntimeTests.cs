using System;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using Knight.Abilities;
using Knight.Combat;
using Knight.Enemies.Laborer;
using Knight.Mechanisms;
using Knight.Player;
using Knight.Prologue;
using Knight.Shared;
using Knight.Vfx;

namespace Knight.Tests.Integration;

public static partial class BellChainRuntimeTests
{
    public static async Task Run(SceneTree tree)
    {
        await LaborerArmorExposureAndResetUseRealScene(tree);
        await MechanicalClawHonorsRangeAndWorldOcclusion(tree);
        await RealMechanicalClawInputOpensMechanismOnceAndProgressSurvivesReload(tree);
    }

    private static async Task LaborerArmorExposureAndResetUseRealScene(SceneTree tree)
    {
        TestAssert.True(ResourceLoader.Exists("res://scenes/enemies/BellBackLaborer.tscn"),
            "formal Bell-Back Laborer scene exists");
        var root = new Node2D { Name = "LaborerRuntimeFixture" };
        var floor = new StaticBody2D { Name = "Floor", Position = new Vector2(0, 120), CollisionLayer = 1 };
        floor.AddChild(new CollisionShape2D { Name = "Collision", Shape = new RectangleShape2D { Size = new Vector2(700, 40) } });
        CharacterBody2D player = GD.Load<PackedScene>("res://scenes/player/Player.tscn").Instantiate<CharacterBody2D>();
        player.Position = new Vector2(0, 68); player.AddToGroup("player"); player.GetNode<Camera2D>("Camera2D").Enabled = false;
        BellBackLaborer laborer = GD.Load<PackedScene>("res://scenes/enemies/BellBackLaborer.tscn").Instantiate<BellBackLaborer>();
        laborer.Position = new Vector2(70, 68);
        root.AddChild(floor); root.AddChild(player); root.AddChild(laborer); tree.Root.AddChild(root);
        await Frames(tree, 3);
        TestAssert.Equal(BellBackLaborerState.Guard, laborer.CurrentState, "nearby player brings real laborer into Guard");
        HealthComponent health = laborer.GetNode<HealthComponent>("Health");
        Hurtbox hurtbox = laborer.GetNode<Hurtbox>("Hurtbox");
        int before = health.CurrentHealth;
        for (ulong id = 1; id <= 6; id++)
            TestAssert.True(hurtbox.Receive(new DamageRequest(1, Vector2.Right, 0f, 9000UL + id)),
                $"guarded hit {id} is consumed by armor");
        TestAssert.Equal(before - 2, health.CurrentHealth, "six guarded one-point hits deal exactly two health damage");

        MechanicalClawController claw = player.GetNode<MechanicalClawController>("MechanicalClaw");
        ThreadTarget2D laborerTarget = laborer.GetNode<ThreadTarget2D>("ThreadTarget");
        LegacyThreadInteractionAdapter laborerAdapter =
            laborerTarget.GetNode<LegacyThreadInteractionAdapter>("LegacyThreadInteractionAdapter");
        MechanicalClawFrame laborerCast = await CastClaw(tree, player, laborerTarget.WorldAnchor);
        TestAssert.Equal(MechanicalClawState.Released, laborerCast.State,
            "heavy laborer adapter applies and releases without publishing a persistent latch");
        await WaitFor(tree, () => laborer.CurrentState == BellBackLaborerState.Exposed,
            "heavy laborer legacy-adapter Apply");
        TestAssert.Equal(BellBackLaborerState.Exposed, laborer.CurrentState,
            "real player mechanical claw reaches the heavy laborer tutorial receiver");
        TestAssert.Equal(1, laborerAdapter.ApplicationCount,
            "real player mechanical claw applies the heavy interaction once");

        Vector2 priorVelocity = laborer.Velocity;
        TestAssert.True(laborer.TryReceiveThread(new ThreadInteraction(player, player.GlobalPosition,
            laborer.GlobalPosition, new Knight.Abilities.ThreadAbilitySettings())),
            "heavy laborer accepts generic thread interaction");
        await Frames(tree, 1);
        TestAssert.Equal(BellBackLaborerState.Exposed, laborer.CurrentState, "thread opens real exposed state");
        TestAssert.Near(priorVelocity.X, laborer.Velocity.X, .001f, "thread does not pull heavy body horizontally");
        int exposedBefore = health.CurrentHealth;
        TestAssert.True(hurtbox.Receive(new DamageRequest(1, Vector2.Right, 0f, 9100UL)),
            "exposed hit is accepted");
        TestAssert.Equal(exposedBefore - 1, health.CurrentHealth, "exposed hit bypasses armor accumulator");

        var fallbackLightReceiver = new LightPullSpy { Name = "Receiver" };
        var fallbackLight = new ThreadTarget2D {
            Name = "FallbackLight", Kind = ThreadTargetKind.LightEnemy, PullReceiverPath = new NodePath("Receiver")
        };
        fallbackLight.AddChild(fallbackLightReceiver);
        var fallbackCollider = new Area2D { Name = "TargetCollision", CollisionLayer = CombatCollisionLayers.ThreadTarget };
        fallbackCollider.AddChild(new CollisionShape2D { Name = "Collision", Shape = new CircleShape2D { Radius = 12f } });
        fallbackLight.AddChild(fallbackCollider);
        fallbackLight.AddChild(new LegacyThreadInteractionAdapter
        {
            Name = "LegacyThreadInteractionAdapter"
        });
        root.AddChild(fallbackLight);
        fallbackLight.GlobalPosition = player.GlobalPosition + new Vector2(110f, -130f);
        await Frames(tree, 1);
        TestAssert.True(laborerTarget.IsInGroup("thread_targets") && fallbackLight.IsInGroup("thread_targets"),
            "live tutorial target and valid light fallback coexist before the laborer dies");

        health.ApplyDamage(new DamageRequest(health.CurrentHealth, Vector2.Right, 0f, 9200UL));
        await Frames(tree, 1);
        TestAssert.True(!laborerTarget.IsInGroup("thread_targets"),
            "a dead heavy tutorial target is removed from the real thread target group");

        player.Velocity = Vector2.Zero;
        MechanicalClawFrame fallbackCast = await CastClaw(tree, player, fallbackLight.WorldAnchor);
        TestAssert.Equal(MechanicalClawState.Released, fallbackCast.State,
            "light fallback adapter applies and releases without a persistent latch");
        await WaitFor(tree, () => fallbackLightReceiver.Calls == 1,
            "light fallback legacy-adapter Apply after heavy target death");
        TestAssert.Equal(1, fallbackLightReceiver.Calls,
            "a valid light target receives the player thread after the dead heavy target leaves the group");
        TestAssert.Near(0f, player.Velocity.X, .001f,
            "the dead heavy target cannot fall back to pulling the player or consume the interaction");

        laborer.ResetForCheckpoint();
        TestAssert.Equal(BellBackLaborerState.Idle, laborer.CurrentState, "laborer reset restores idle-ready model");
        TestAssert.Equal(health.MaxHealth, health.CurrentHealth, "laborer reset restores full health");
        TestAssert.Equal(new NodePath("../Armor"), hurtbox.DamageReceiverPath, "laborer reset restores guarded damage receiver");
        TestAssert.True(laborerTarget.IsInGroup("thread_targets"),
            "checkpoint reset restores the heavy tutorial target to real thread selection");
        MechanicalClawFrame restoredCast = await CastClaw(tree, player, laborerTarget.WorldAnchor);
        TestAssert.Equal(MechanicalClawState.Released, restoredCast.State,
            "restored heavy target applies and releases without a persistent latch");
        await WaitFor(tree, () => laborer.CurrentState == BellBackLaborerState.Exposed,
            "restored heavy tutorial target real thread Apply");
        TestAssert.Equal(BellBackLaborerState.Exposed, laborer.CurrentState,
            "the restored tutorial target reaches exposed through the real player thread path");
        await Dispose(tree, root);
    }

    private static async Task RealMechanicalClawInputOpensMechanismOnceAndProgressSurvivesReload(
        SceneTree tree)
    {
        PrologueProgress progress = new();
        MechanismFixture fixture = await MechanismRoom(tree, "MechanismInputFixture", progress);
        int opened = 0;
        fixture.Mechanism.Opened += () => opened++;
        fixture.Player.GlobalPosition = fixture.Mechanism.GlobalPosition + new Vector2(-190, 40);
        ThreadTarget2D mechanismTarget = fixture.Mechanism.GetNode<ThreadTarget2D>("ThreadTarget");
        var nearerAnchor = new ThreadTarget2D { Name = "NearerAnchor", Kind = ThreadTargetKind.Anchor };
        AddTargetCollision(nearerAnchor);
        fixture.Root.AddChild(nearerAnchor);
        nearerAnchor.GlobalPosition = mechanismTarget.WorldAnchor + new Vector2(-18f, 29f);
        await Frames(tree, 1);
        LegacyThreadInteractionAdapter adapter = mechanismTarget.GetNode<LegacyThreadInteractionAdapter>(
            "LegacyThreadInteractionAdapter");
        MechanicalClawFrame firstTerminal = await CastClaw(tree, fixture.Player,
            mechanismTarget.WorldAnchor);
        TestAssert.Equal(MechanicalClawState.Released, firstTerminal.State,
            "first directed mechanism cast applies then releases rather than retaining a latch");
        TestAssert.Equal(1, adapter.ApplicationCount,
            "first directed mechanism cast applies its adapter once");
        TestAssert.True(fixture.Mechanism.IsOpen && !progress.BellChainShortcutOpen,
            "mechanism emits its domain event without mutating continuous progress authority");
        TestAssert.True(fixture.Mechanism.GetNode<CollisionShape2D>("Door/Collision").Disabled,
            "opened mechanism disables shortcut door collision");
        TestAssert.Equal(1, opened, "first real thread use emits one Opened signal");
        TestAssert.True(progress.OpenBellChainShortcut(),
            "the isolated fixture applies the single LevelController-owned shortcut mutation");
        fixture.Mechanism.ApplyProgress(progress);

        MechanicalClawFrame secondTerminal = await CastClaw(tree, fixture.Player,
            mechanismTarget.WorldAnchor);
        TestAssert.Equal(MechanicalClawState.Released, secondTerminal.State,
            "second directed mechanism cast applies then releases without a persistent latch");
        TestAssert.Equal(2, adapter.ApplicationCount,
            "second directed mechanism cast applies the adapter a second time");
        TestAssert.Equal(1, opened, "repeated real thread use never emits Opened again");

        Node actorRoot = fixture.Root.GetNode("Actors");
        Node oldLaborer = actorRoot.GetNode("laborer");
        ulong oldLaborerId = oldLaborer.GetInstanceId();
        oldLaborer.GetNode<HealthComponent>("Health").ApplyDamage(
            new DamageRequest(3, Vector2.Right, 0f, 9200UL));
        fixture.Root.ResetRoomState();
        await Frames(tree, 2);
        BellBackLaborer resetLaborer = actorRoot.GetNode<BellBackLaborer>("laborer");
        TestAssert.True(fixture.Mechanism.IsOpen && progress.BellChainShortcutOpen &&
            fixture.Mechanism.GetNode<CollisionShape2D>("Door/Collision").Disabled,
            "checkpoint reset preserves the opened permanent shortcut");
        TestAssert.True(resetLaborer.GetInstanceId() != oldLaborerId,
            "checkpoint reset replaces the laborer encounter instance");
        TestAssert.Equal(resetLaborer.GetNode<HealthComponent>("Health").MaxHealth,
            resetLaborer.GetNode<HealthComponent>("Health").CurrentHealth,
            "checkpoint reset restores laborer full health");
        TestAssert.Equal(new NodePath("../Armor"), resetLaborer.GetNode<Hurtbox>("Hurtbox").DamageReceiverPath,
            "checkpoint reset restores laborer guarded armor routing");
        await Dispose(tree, fixture.Root);

        MechanismFixture reloaded = await MechanismRoom(tree, "MechanismReloadFixture", progress);
        TestAssert.True(reloaded.Mechanism.IsOpen && reloaded.Mechanism.GetNode<CollisionShape2D>("Door/Collision").Disabled,
            "persisted shortcut applies to a freshly loaded room after recovery");
        await Dispose(tree, reloaded.Root);

        MechanismFixture fresh = await MechanismRoom(tree, "MechanismFreshFixture", new PrologueProgress());
        TestAssert.True(!fresh.Mechanism.IsOpen && !fresh.Mechanism.GetNode<CollisionShape2D>("Door/Collision").Disabled,
            "fresh prologue progress starts with shortcut closed");
        await Dispose(tree, fresh.Root);
    }

    private static async Task MechanicalClawHonorsRangeAndWorldOcclusion(SceneTree tree)
    {
        PrologueProgress progress = new();
        MechanismFixture fixture = await MechanismRoom(tree, "NeutralAssistFixture", progress);
        ThreadLineVisual line = fixture.Player.GetNode<ThreadLineVisual>("VisualRoot/MechanicalClaw/Line");
        ThreadTarget2D target = fixture.Mechanism.GetNode<ThreadTarget2D>("ThreadTarget");
        LegacyThreadInteractionAdapter adapter = target.GetNode<LegacyThreadInteractionAdapter>(
            "LegacyThreadInteractionAdapter");

        float beyondRange = MechanicalClawSettings.Default.MaxRange + 1f;
        fixture.Player.GlobalPosition = target.GlobalPosition + new Vector2(-beyondRange, 0f);
        MechanicalClawFrame far = await CastClaw(tree, fixture.Player, target.WorldAnchor);
        TestAssert.Equal(MechanicalClawState.Failed, far.State,
            "out-of-range mechanical-claw cast reaches a named failure");
        TestAssert.Equal(MechanicalClawFailure.OutOfRange, far.Failure,
            $"{beyondRange:0}-pixel mechanical-claw cast preserves the fixed range boundary");
        TestAssert.True(!fixture.Mechanism.IsOpen && adapter.ApplicationCount == 0,
            "out-of-range mechanical claw does not open the mechanism");

        fixture.Player.GlobalPosition = target.GlobalPosition + new Vector2(-220f, 0f);
        var wall = new StaticBody2D {
            Name = "AssistOccluder", CollisionLayer = CombatCollisionLayers.World, CollisionMask = 0
        };
        wall.AddChild(new CollisionShape2D {
            Name = "Collision", Shape = new RectangleShape2D { Size = new Vector2(32f, 160f) }
        });
        fixture.Root.AddChild(wall);
        wall.GlobalPosition = target.GlobalPosition + new Vector2(-110f, 0f);
        await Frames(tree, 2);
        MechanicalClawFrame blocked = await CastClaw(tree, fixture.Player, target.WorldAnchor);
        TestAssert.Equal(MechanicalClawFailure.Obstructed, blocked.Failure,
            "occluded mechanical-claw cast terminates with the real obstruction failure");
        TestAssert.True(!fixture.Mechanism.IsOpen && adapter.ApplicationCount == 0,
            "mechanical claw cannot target through a real world-layer wall");

        wall.Free();
        await Frames(tree, 1);
        fixture.Player.GlobalPosition = target.GlobalPosition + new Vector2(-220f, 0f);
        MechanicalClawFrame applied = await CastClaw(tree, fixture.Player, target.WorldAnchor);
        TestAssert.Equal(MechanicalClawState.Released, applied.State,
            "valid mechanical-claw cast applies then releases without a persistent latch");
        TestAssert.True(fixture.Mechanism.IsOpen && !progress.BellChainShortcutOpen &&
            adapter.ApplicationCount == 1,
            "valid mechanical claw emits the real mechanism interaction without owning progress");
        TestAssert.Equal(0, line.ActiveStrandCount,
            "legacy mechanism interaction leaves no persistent rope visual");

        await Dispose(tree, fixture.Root);
    }

    private static async Task<MechanicalClawFrame> CastClaw(SceneTree tree,
        CharacterBody2D player, Vector2 mouseWorld)
    {
        PlayerInputReader input = player.GetNode<PlayerInputReader>("Input");
        PlayerActionModeController modes = player.GetNode<PlayerActionModeController>("ActionModes");
        MechanicalClawController claw = player.GetNode<MechanicalClawController>("MechanicalClaw");
        MechanicalClawFrame terminal = default;
        bool completed = false;
        void OnFrame(MechanicalClawFrame frame, GrappleSurface2D surface)
        {
            if (frame.State is not (MechanicalClawState.Released or MechanicalClawState.Failed)) return;
            terminal = frame;
            completed = true;
        }

        ReleaseClawInput();
        claw.ResetTransientState();
        input.SetPhysicsProcess(false);
        modes.SetPhysicsProcess(false);
        claw.SetPhysicsProcess(false);
        claw.ClawFrameChanged += OnFrame;
        try
        {
            Input.ActionPress(InputActions.SelectGrappleMode);
            Input.ActionPress(InputActions.PrimaryAction);
            for (int frame = 0; frame < 180 && !completed; frame++)
            {
                SampleClawInput(input, modes, claw, mouseWorld);
                await Frames(tree, 1);
                if (frame == 0) Input.ActionRelease(InputActions.SelectGrappleMode);
            }
            if (!completed) throw new InvalidOperationException(
                $"Timed out waiting for mechanical claw legacy release or failure; current={claw.CurrentFrame.State}");
            return terminal;
        }
        finally
        {
            ReleaseClawInput();
            claw.ClawFrameChanged -= OnFrame;
            input.SetPhysicsProcess(true);
            modes.SetPhysicsProcess(true);
            claw.SetPhysicsProcess(true);
            await Frames(tree, 1);
        }
    }

    private static void SampleClawInput(PlayerInputReader input, PlayerActionModeController modes,
        MechanicalClawController claw, Vector2 mouseWorld)
    {
        input._PhysicsProcess(0d);
        PlayerInputFrame frame = input.CurrentFrame with { MouseWorld = mouseWorld };
        typeof(PlayerInputReader).GetProperty(nameof(PlayerInputReader.CurrentFrame),
            BindingFlags.Instance | BindingFlags.Public)!.SetValue(input, frame);
        modes._PhysicsProcess(0d);
        claw._PhysicsProcess(1d / 60d);
    }

    private static void AddTargetCollision(GrappleSurface2D target)
    {
        var area = new Area2D
        {
            Name = "TargetCollision",
            CollisionLayer = CombatCollisionLayers.ThreadTarget,
            CollisionMask = CombatCollisionLayers.None
        };
        area.AddChild(new CollisionShape2D
        {
            Name = "Collision",
            Shape = new CircleShape2D { Radius = 12f }
        });
        target.AddChild(area);
    }

    private static void ReleaseClawInput()
    {
        Input.ActionRelease(InputActions.SelectGrappleMode);
        Input.ActionRelease(InputActions.PrimaryAction);
        Input.ActionRelease(InputActions.MoveLeft);
        Input.ActionRelease(InputActions.MoveRight);
        Input.ActionRelease(InputActions.MoveUp);
        Input.ActionRelease(InputActions.MoveDown);
    }

    private static async Task<MechanismFixture> MechanismRoom(SceneTree tree, string name, PrologueProgress progress)
    {
        PrologueRoom room = GD.Load<PackedScene>("res://scenes/prologue/rooms/BellChainMechanism.tscn").Instantiate<PrologueRoom>();
        room.Name = name;
        CharacterBody2D player = GD.Load<PackedScene>("res://scenes/player/Player.tscn").Instantiate<CharacterBody2D>();
        player.Name = "RuntimePlayer"; player.AddToGroup("player"); player.GetNode<Camera2D>("Camera2D").Enabled = false;
        room.AddChild(player);
        tree.Root.AddChild(room);
        BellChainMechanism mechanism = room.GetNode<BellChainMechanism>("Mechanisms/BellChain");
        mechanism.ApplyProgress(progress);
        await Frames(tree, 2);
        return new MechanismFixture(room, player, mechanism);
    }

    private static async Task Frames(SceneTree tree, int count)
    {
        for (int i = 0; i < count; i++) await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
    }

    private static async Task WaitFor(SceneTree tree, Func<bool> predicate, string operation)
    {
        for (int index = 0; index < 180 && !predicate(); index++) await Frames(tree, 1);
        if (!predicate()) throw new InvalidOperationException($"Timed out waiting for {operation}");
    }

    private static async Task Dispose(SceneTree tree, Node root)
    {
        ReleaseClawInput();
        root.QueueFree(); await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
    }

    private sealed record MechanismFixture(PrologueRoom Root, CharacterBody2D Player, BellChainMechanism Mechanism);

    private sealed partial class LightPullSpy : Node2D, IThreadPullReceiver
    {
        public int Calls { get; private set; }
        public Vector2 Velocity { get; private set; }

        public void ApplyThreadPull(Vector2 velocity)
        {
            Calls++;
            Velocity = velocity;
        }
    }
}
