using System.Threading.Tasks;
using Godot;
using Knight.Build.Prologue;
using Knight.Combat;
using Knight.Player;
using Knight.Prologue;
using Knight.Prologue.Seamless;
using Knight.Shared;
using Knight.Presentation;
using Knight.Abilities;

namespace Knight.Tests.Integration;

// Missing landing damage, terminal-speed-only damage, or repeat-on-floor damage breaks
// these assertions. All motion uses the packed production player and real world collision.
public static class PlayerFallDamageRuntimeTests
{
    public static async Task RunGrappleLandingRegression(SceneTree tree)
    {
        await RealGrappleBrakesALateHighFall(tree, tooLate: true);
        await RealGrappleBrakesALateHighFall(tree, tooLate: false);
    }

    public static async Task RunPresentation(SceneTree tree)
    {
        await Drop(tree, 400f, 5);
        await Drop(tree, 800f, 4);
        await Drop(tree, 1600f, 2);
        await GeneratedAbyssHasOnlyAnExplicitSoftLanding(tree, presentationHoldFrames: 60);
    }

    public static async Task Run(SceneTree tree)
    {
        await Drop(tree, 800f, 4);
        await Drop(tree, 400f, 5);
        await Drop(tree, 1200f, 3);
        await Drop(tree, 1600f, 2);
        await Drop(tree, 2400f, 0);
        await Drop(tree, 800f, 4, behavior: "pause");
        await Drop(tree, 800f, 4, behavior: "dash");
        await Drop(tree, 800f, 4, behavior: "shield");
        await Drop(tree, 800f, 4, behavior: "invulnerability");
        await Drop(tree, 3200f, 4, softLanding: true);
        await RealGrappleBrakesALateHighFall(tree, tooLate: true);
        await RealGrappleBrakesALateHighFall(tree, tooLate: false);
        await GeneratedAbyssHasOnlyAnExplicitSoftLanding(tree);
    }

    private static async Task RealGrappleBrakesALateHighFall(SceneTree tree, bool tooLate)
    {
        var root = new Node2D { Name = "ActualClawBrakingFixture" };
        var floor = new StaticBody2D
        {
            Position = new Vector2(320f, 2020f), CollisionLayer = CombatCollisionLayers.World
        };
        floor.AddChild(new CollisionShape2D
        {
            Shape = new RectangleShape2D { Size = new Vector2(1600f, 40f) }
        });
        root.AddChild(floor);
        var anchor = new GrappleSurface2D
        {
            Position = new Vector2(520f, 1450f), Material = GrappleSurfaceMaterial.DedicatedAnchor
        };
        var area = new Area2D { CollisionLayer = CombatCollisionLayers.ThreadTarget };
        area.AddChild(new CollisionShape2D { Shape = new CircleShape2D { Radius = 12f } });
        anchor.AddChild(area);
        root.AddChild(anchor);
        CharacterBody2D player = GD.Load<PackedScene>("res://scenes/player/Player.tscn")
            .Instantiate<CharacterBody2D>();
        player.Position = new Vector2(320f, 100f);
        player.GetNode<Camera2D>("Camera2D").Enabled = false;
        root.AddChild(player);
        tree.Root.AddChild(root);
        player.GetNode<Camera2D>("Camera2D").ForceUpdateScroll();
        PlayerInputReader input = player.GetNode<PlayerInputReader>("Input");
        MechanicalClawController claw = player.GetNode<MechanicalClawController>("MechanicalClaw");
        bool sawAttachment = false;
        MechanicalClawState lastState = MechanicalClawState.Idle;
        claw.ClawFrameChanged += (frame, _) =>
        {
            sawAttachment |= frame.State is MechanicalClawState.Latched or MechanicalClawState.Reel or MechanicalClawState.Swing;
            if (frame.State != lastState)
                GD.Print($"FALL_CLAW_TRACE state={frame.State} failure={frame.Failure} player={player.Position} velocity={player.Velocity}");
            lastState = frame.State;
        };
        try
        {
            int ticks = 0;
            float castHeight = tooLate ? 1560f : 1300f;
            while (player.Position.Y < castHeight && ticks++ < 200) await Sample(tree, input);
            TestAssert.True(player.Position.Y >= castHeight && player.Velocity.Y > 700f,
                "claw braking begins after a genuine >1000-unit terminal-speed descent");
            await Dispatch(tree, input, InputActions.SelectGrappleMode, true);
            await Dispatch(tree, input, InputActions.SelectGrappleMode, false);
            TestAssert.Equal(PlayerActionMode.Grapple,
                player.GetNode<PlayerActionModeController>("ActionModes").CurrentMode,
                "the real mode key enables claw casting before this mitigation check");
            Vector2 viewport = player.GetViewport().GetFinalTransform() *
                (player.GetGlobalTransformWithCanvas() * player.ToLocal(anchor.GlobalPosition));
            var motion = new InputEventMouseMotion { Position = viewport, GlobalPosition = viewport };
            player.GetViewport().PushInput(motion, true);
            Input.ParseInputEvent(motion);
            Input.FlushBufferedEvents();
            await Sample(tree, input);
            TestAssert.Near(0f, input.Current.MouseWorld.DistanceTo(anchor.GlobalPosition), .1f,
                "the real pointer event selects the authored braking anchor");
            await Dispatch(tree, input, InputActions.PrimaryAction, true);
            await Dispatch(tree, input, InputActions.PrimaryAction, false);
            ticks = 0;
            while (claw.CurrentFrame.State != MechanicalClawState.Swing && ticks++ < 120)
                await Sample(tree, input);
            if (tooLate)
            {
                TestAssert.True(sawAttachment && player.IsOnFloor(),
                    "the very late claw really attaches, but reaches the floor before arresting descent");
                TestAssert.Equal(1, player.GetNode<HealthComponent>("Health").CurrentHealth,
                    "attachment/casting alone cannot erase a still-fast huge fall");
                GD.Print("FALL_LATE_CLAW_DAMAGE_OK health=1");
                return;
            }
            TestAssert.Equal(MechanicalClawState.Swing, claw.CurrentFrame.State,
                "a real valid claw arrests the fall and reaches its suspended arrival");
            for (int index = 0; index < 12; index++) await Sample(tree, input);
            await Dispatch(tree, input, InputActions.PrimaryAction, true);
            await Dispatch(tree, input, InputActions.PrimaryAction, false);
            ticks = 0;
            while (!player.IsOnFloor() && ticks++ < 200) await Sample(tree, input);
            TestAssert.True(player.IsOnFloor(), "ordinary cancellation lands on the unpadded floor");
            TestAssert.Equal(5, player.GetNode<HealthComponent>("Health").CurrentHealth,
                "genuine braking removes the preceding huge fall, not merely the cast key");
            GD.Print($"FALL_REAL_CLAW_BRAKE_OK landed={player.Position} health=5");
        }
        finally
        {
            Input.ActionRelease(InputActions.PrimaryAction);
            Input.ActionRelease(InputActions.SelectGrappleMode);
            root.QueueFree();
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
        }
    }

    private static async Task Sample(SceneTree tree, PlayerInputReader input)
    {
        ulong before = input.CurrentFrameSequence;
        for (int index = 0; index < 6 && before == input.CurrentFrameSequence; index++)
            await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
        TestAssert.True(input.CurrentFrameSequence > before, "real production input sampling advances");
    }

    private static async Task Dispatch(SceneTree tree, PlayerInputReader input, StringName action, bool pressed)
    {
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        Input.ParseInputEvent(new InputEventAction { Action = action, Pressed = pressed, Strength = pressed ? 1f : 0f });
        Input.FlushBufferedEvents();
        await Sample(tree, input);
    }

    private static async Task Drop(SceneTree tree, float height, int expectedHealth,
        bool softLanding = false, string behavior = "normal")
    {
        var root = new Node2D { Name = "FallDamageFixture" };
        // This fixture tests injury after a real landing, not horizontal range.
        // The finite jet's coast passes the old 640px pad; retain a real floor
        // wide enough to land without cancelling force or disabling collision.
        float landingWidth = behavior == "dash" ? 1600f : 640f;
        var floor = new StaticBody2D
        {
            Position = new Vector2(320f, 3000f + 20f),
            CollisionLayer = CombatCollisionLayers.World,
            CollisionMask = 0
        };
        floor.AddChild(new CollisionShape2D
        {
            Shape = new RectangleShape2D { Size = new Vector2(landingWidth, 40f) }
        });
        if (softLanding) floor.SetMeta("soft_landing", true);
        floor.AddChild(new CanyonPlatformVisual
        {
            Name = "FloorSkin", SkinSize = new Vector2(landingWidth, 40f),
            SurfaceMaterial = Knight.Abilities.GrappleSurfaceMaterial.GripStone,
            SectorId = PrologueSectorId.CanyonBasin
        });
        root.AddChild(floor);
        CharacterBody2D player = GD.Load<PackedScene>("res://scenes/player/Player.tscn")
            .Instantiate<CharacterBody2D>();
        player.Position = new Vector2(320f, 3000f - 36f - height);
        root.AddChild(player);
        HealthComponent health = player.GetNode<HealthComponent>("Health");
        int damageEvents = 0;
        health.Damaged += (_, _) => damageEvents++;
        tree.Root.AddChild(root);
        PlayerInputReader input = player.GetNode<PlayerInputReader>("Input");
        input.SetPhysicsProcess(behavior is "dash" or "shield");
        if (softLanding)
            health.ReceiveDamage(new DamageRequest(1, Vector2.Zero, 0f, 1UL, floor.GetInstanceId()));
        try
        {
            int frames = 0;
            bool reportedDashFloorCrossing = false;
            bool sawActiveShield = false;
            bool requestedShield = false;
            while (!player.IsOnFloor() && frames++ < 400)
            {
                if (behavior == "pause" && frames == 35)
                {
                    Vector2 beforePause = player.Position;
                    tree.Paused = true;
                    for (int index = 0; index < 10; index++)
                        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
                    TestAssert.Equal(beforePause, player.Position, "pause suspends physical descent");
                    tree.Paused = false;
                }
                if (behavior == "dash" && frames == 35)
                {
                    await Dispatch(tree, input, InputActions.Dash, true);
                    await Dispatch(tree, input, InputActions.Dash, false);
                }
                if (player.Position.Y > 2860f && behavior == "shield" && !requestedShield)
                {
                    requestedShield = true;
                    await Dispatch(tree, input, InputActions.Shield, true);
                }
                if (player.Position.Y > 2860f && behavior == "invulnerability" && damageEvents == 0)
                    health.ReceiveDamage(new DamageRequest(1, Vector2.Left, 0f, 30UL, floor.GetInstanceId()));
                await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
                sawActiveShield |= player.GetNode<PlayerShieldController>("Shield").IsActive;
                if (behavior == "dash" && !reportedDashFloorCrossing && player.Position.Y >= 2964f)
                {
                    reportedDashFloorCrossing = true;
                    GD.Print($"FALL_DASH_FLOOR_CROSSING position={player.Position} velocity={player.Velocity} floor_left={320f - landingWidth / 2f} floor_right={320f + landingWidth / 2f} on_floor={player.IsOnFloor()}");
                }
            }
            TestAssert.True(player.IsOnFloor(),
                $"drop {height} reaches a physical floor; behavior={behavior} position={player.Position} velocity={player.Velocity}");
            await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
            TestAssert.Equal(expectedHealth, health.CurrentHealth,
                $"drop {height} applies height-scaled landing damage");
            TestAssert.Equal(expectedHealth < 5 ? 1 : 0, damageEvents,
                $"drop {height} emits the expected injury count including the fixture's initial hit");
            if (behavior == "dash")
                TestAssert.Equal(1, player.GetNode<PlayerMotor>("Motor").AcceptedDashCount,
                    "an actual accepted dash does not erase accumulated fall height");
            if (behavior == "shield")
            {
                TestAssert.True(sawActiveShield, "the defense test actually opens a real energy shield");
                TestAssert.True(player.GetNode<PlayerShieldController>("Shield").IsActive,
                    "an active held shield does not absorb fall damage or change its existing sustain policy");
            }
            for (int index = 0; index < 30; index++)
                await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
            TestAssert.Equal(expectedHealth, health.CurrentHealth,
                "remaining on the floor cannot repeat fall damage");
            if (softLanding)
            {
                TestAssert.True(health.ReceiveDamage(new DamageRequest(1, Vector2.Left, 0f,
                        2UL, floor.GetInstanceId())), "a soft landing grants no combat immunity");
                TestAssert.Equal(3, health.CurrentHealth, "soft landing never heals existing injuries");
            }
            GD.Print($"FALL_DROP_OK height={height} health={health.CurrentHealth} events={damageEvents} behavior={behavior} soft={softLanding}");
        }
        finally
        {
            tree.Paused = false;
            Input.ActionRelease(InputActions.Dash);
            Input.ActionRelease(InputActions.Shield);
            root.QueueFree();
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
        }
    }

    private static async Task GeneratedAbyssHasOnlyAnExplicitSoftLanding(SceneTree tree, int presentationHoldFrames = 0)
    {
        PrologueSector sector = GD.Load<PackedScene>(
            "res://scenes/prologue/sectors/CanyonDescent.tscn").Instantiate<PrologueSector>();
        StaticBody2D bottom = sector.GetNode<StaticBody2D>("Gameplay/Collision/AbyssSafeBottom");
        TestAssert.True(bottom.HasMeta("soft_landing") && bottom.GetMeta("soft_landing").AsBool(),
            "actual generated abyss bottom has an explicit soft landing");
        int softCount = 0;
        foreach (Node child in sector.GetNode("Gameplay/Collision").GetChildren())
            if (child.HasMeta("soft_landing") && child.GetMeta("soft_landing").AsBool()) softCount++;
        TestAssert.Equal(1, softCount, "catch shelves do not silently become immune to falling");
        RectangleShape2D shape = (RectangleShape2D)bottom.GetNode<CollisionShape2D>("Collision").Shape;
        TestAssert.Equal(new Vector2(640f, 40f), shape.Size, "soft landing leaves the authored support unchanged");
        TestAssert.True(sector.GetNodeOrNull<Node2D>(
                "VisualRoot/GameplayReadable/AbyssSafeBottom/SoftLandingPad") != null,
            "the protection has a visible padded surface, not hidden immunity");

        // Use the actual saved collision, not a duplicated test collider. Its world top is
        // 3560; starting at 500 reproduces >3000 units of unavoidable central descent.
        var root = new Node2D { Name = "GeneratedAbyssLandingFixture" };
        Node2D skin = sector.GetNode<Node2D>("VisualRoot/GameplayReadable/AbyssSafeBottom");
        ClearOwners(skin);
        skin.GetParent().RemoveChild(skin);
        skin.Position += CanyonWorldLayout.SectorBounds[PrologueSectorId.CanyonDescent].Position;
        root.AddChild(skin);
        ClearOwners(bottom);
        sector.GetNode("Gameplay/Collision").RemoveChild(bottom);
        bottom.Position += CanyonWorldLayout.SectorBounds[PrologueSectorId.CanyonDescent].Position;
        root.AddChild(bottom);
        sector.Free();
        CharacterBody2D player = GD.Load<PackedScene>("res://scenes/player/Player.tscn")
            .Instantiate<CharacterBody2D>();
        player.Position = new Vector2(2240f, 500f);
        root.AddChild(player);
        tree.Root.AddChild(root);
        player.GetNode<PlayerInputReader>("Input").SetPhysicsProcess(false);
        try
        {
            int frames = 0;
            while (!player.IsOnFloor() && frames++ < 400)
                await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
            TestAssert.True(player.IsOnFloor() && frames >= 240 && frames <= 360,
                $"unassisted abyss landing retains 4–6 seconds, actual frames={frames}");
            TestAssert.Equal(5, player.GetNode<HealthComponent>("Health").CurrentHealth,
                "the real mandatory abyss remains survivable without casting/jumping");
            GD.Print($"FALL_GENERATED_ABYSS_OK frames={frames} health=5");
            for (int index = 0; index < presentationHoldFrames; index++)
                await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
        }
        finally
        {
            root.QueueFree();
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
        }
    }

    private static void ClearOwners(Node node)
    {
        foreach (Node child in node.GetChildren()) ClearOwners(child);
        node.Owner = null;
    }
}
