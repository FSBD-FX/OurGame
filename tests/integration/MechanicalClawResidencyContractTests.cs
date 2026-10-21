using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using Knight.Abilities;
using Knight.Combat;
using Knight.Player;

namespace Knight.Tests.Integration;

public static class MechanicalClawResidencyContractTests
{
    private const float Frame = 1f / 60f;
    private static ulong _damageId = 975000UL;

    public static async Task Run(SceneTree tree)
    {
        await TerrainTravelPinsOnceAndReleaseUnpinsOnce(tree);
        await AimWindupAndLegacyTargetsNeverPin(tree);
        await CancelBoostDashShieldAndQUnpinBeforeTargetReferenceClears(tree);
        await DamageDeathResetAndExitTreeUnpinExactlyOnce(tree);
        await NotifyTargetWillUnloadUnpinsThenClearsSynchronously(tree);
        await SubscriberCanReadResidencyTargetAfterLateBinding(tree);
        await ThrowingSubscriberCannotPreventCleanupOrOtherSubscribers(tree);
        await ResetInsidePinOrUnpinCallbackDoesNotRecurseOrRepin(tree);
        GD.Print("MECHANICAL_CLAW_RESIDENCY_CONTRACT_OK");
    }

    private static async Task TerrainTravelPinsOnceAndReleaseUnpinsOnce(SceneTree tree)
    {
        Fixture fixture = await CreateFixture(tree, "TerrainTravel", physicalTerrain: true);
        var transitions = new List<(GrappleSurface2D Target, bool Resident)>();
        try
        {
            fixture.Claw.TargetResidencyChanged += Observe;
            AdvanceToTravel(fixture);
            TestAssert.True(transitions.Count == 1 && transitions[0] == (fixture.Surface, true),
                "terrain first entering Travel publishes exactly one residency pin");

            Step(fixture, Input(primaryPressed: true, mouseWorld: fixture.AimWorld));
            TestAssert.True(transitions.Count == 2 && transitions[1] == (fixture.Surface, false),
                "ordinary release publishes exactly one matching residency unpin");
            TestAssert.True(fixture.Claw.ResidencyTarget == null && fixture.Claw.CurrentTarget == null,
                "release clears both residency and ordinary target references after unpin");
        }
        finally
        {
            fixture.Claw.TargetResidencyChanged -= Observe;
            await Dispose(tree, fixture.Root);
        }

        void Observe(GrappleSurface2D target, bool resident)
        {
            if (!resident)
            {
                TestAssert.True(GodotObject.IsInstanceValid(target),
                    "the unpin callback receives a still-valid target argument");
                TestAssert.Equal(target, fixture.Claw.ResidencyTarget,
                    "the unpin callback runs before the controller drops its residency reference");
                TestAssert.Equal(target, fixture.Claw.CurrentTarget,
                    "the unpin callback runs before the ordinary target reference is cleared");
            }
            transitions.Add((target, resident));
        }
    }

    private static async Task AimWindupAndLegacyTargetsNeverPin(SceneTree tree)
    {
        Fixture preview = await CreateFixture(tree, "PreviewNoPin");
        var previewTransitions = new List<bool>();
        try
        {
            preview.Claw.TargetResidencyChanged += (_, resident) => previewTransitions.Add(resident);
            Step(preview, Input(grappleModePressed: true, primaryPressed: true,
                primaryHeld: true, mouseWorld: preview.AimWorld));
            TestAssert.Equal(MechanicalClawState.Aim, preview.Claw.CurrentFrame.State,
                "the real controller exposes Aim before allocating terrain residency");
            Step(preview, Input(primaryHeld: true, mouseWorld: preview.AimWorld));
            TestAssert.Equal(MechanicalClawState.Windup, preview.Claw.CurrentFrame.State,
                "the real controller exposes Windup before allocating terrain residency");
            TestAssert.True(previewTransitions.Count == 0 && preview.Claw.ResidencyTarget == null,
                "Aim and Windup publish no residency transition");
        }
        finally { await Dispose(tree, preview.Root); }

        Fixture legacy = await CreateFixture(tree, "LegacyNoPin", legacy: true);
        var legacyTransitions = new List<bool>();
        try
        {
            legacy.Claw.TargetResidencyChanged += (_, resident) => legacyTransitions.Add(resident);
            AdvanceToTravel(legacy);
            TestAssert.True(legacy.Claw.CurrentFrame.State == MechanicalClawState.Travel &&
                legacy.Claw.CurrentFrame.LockedTargetRuntimeId == legacy.Surface.GetInstanceId(),
                "the legacy fixture allocates the real target before residency filtering");
            Step(legacy, Input(primaryPressed: true, mouseWorld: legacy.AimWorld));
            TestAssert.True(legacyTransitions.Count == 0 && legacy.Claw.ResidencyTarget == null,
                "legacy interactions never pin or unpin world residency");
        }
        finally { await Dispose(tree, legacy.Root); }
    }

    private static async Task CancelBoostDashShieldAndQUnpinBeforeTargetReferenceClears(SceneTree tree)
    {
        foreach (ReleaseRoute route in Enum.GetValues<ReleaseRoute>())
        {
            Fixture fixture = await CreateFixture(tree, "Release" + route);
            var order = new List<string>();
            try
            {
                fixture.Claw.TargetResidencyChanged += ObserveResidency;
                fixture.Claw.ClawFrameChanged += ObserveFrame;
                if (route == ReleaseRoute.Boost) AdvanceToAttached(fixture);
                else AdvanceToTravel(fixture);
                order.Clear();

                Step(fixture, route switch
                {
                    ReleaseRoute.Cancel => Input(primaryPressed: true, mouseWorld: fixture.AimWorld),
                    ReleaseRoute.Boost => Input(jumpPressed: true, mouseWorld: fixture.AimWorld),
                    ReleaseRoute.Dash => Input(dashPressed: true, mouseWorld: fixture.AimWorld),
                    ReleaseRoute.Shield => Input(shieldHeld: true, mouseWorld: fixture.AimWorld),
                    ReleaseRoute.Q => Input(attackModePressed: true, mouseWorld: fixture.AimWorld),
                    _ => throw new ArgumentOutOfRangeException()
                });

                int unpin = order.IndexOf("unpin");
                int released = order.IndexOf("Released");
                if (route == ReleaseRoute.Boost)
                    TestAssert.True(order.IndexOf("Boost") >= 0 && order.IndexOf("Boost") < unpin,
                        "Space publishes synthetic Boost before its residency unpin");
                TestAssert.True(unpin >= 0 && released > unpin,
                    $"{route} unpins before publishing Released");
                TestAssert.True(fixture.Claw.ResidencyTarget == null && fixture.Claw.CurrentTarget == null,
                    $"{route} finishes with no target reference");
            }
            finally
            {
                fixture.Claw.TargetResidencyChanged -= ObserveResidency;
                fixture.Claw.ClawFrameChanged -= ObserveFrame;
                await Dispose(tree, fixture.Root);
            }

            void ObserveResidency(GrappleSurface2D target, bool resident)
            {
                if (resident) return;
                TestAssert.Equal(target, fixture.Claw.ResidencyTarget,
                    $"{route} keeps residency readable during unpin");
                TestAssert.Equal(target, fixture.Claw.CurrentTarget,
                    $"{route} unpins before clearing the ordinary target");
                order.Add("unpin");
            }

            void ObserveFrame(MechanicalClawFrame frame, GrappleSurface2D target)
            {
                if (frame.State is MechanicalClawState.Boost or MechanicalClawState.Released)
                    order.Add(frame.State.ToString());
            }
        }
    }

    private static async Task DamageDeathResetAndExitTreeUnpinExactlyOnce(SceneTree tree)
    {
        foreach (ResetRoute route in Enum.GetValues<ResetRoute>())
        {
            Fixture fixture = await CreateFixture(tree, "Reset" + route);
            int pins = 0;
            int unpins = 0;
            try
            {
                fixture.Claw.TargetResidencyChanged += Observe;
                AdvanceToTravel(fixture);
                switch (route)
                {
                    case ResetRoute.Damage:
                        fixture.Health.ApplyDamage(new DamageRequest(1, Vector2.Zero, 0f, ++_damageId));
                        break;
                    case ResetRoute.Death:
                        fixture.Health.ApplyDamage(new DamageRequest(fixture.Health.MaxHealth,
                            Vector2.Zero, 0f, ++_damageId));
                        break;
                    case ResetRoute.Reset:
                        fixture.Claw.ResetTransientState();
                        break;
                    case ResetRoute.ExitTree:
                        fixture.Player.RemoveChild(fixture.Claw);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                TestAssert.True(pins == 1 && unpins == 1,
                    $"{route} publishes one pin and exactly one matching unpin");
                TestAssert.True(fixture.Claw.ResidencyTarget == null && fixture.Claw.CurrentTarget == null,
                    $"{route} clears target references after its once-only unpin");
            }
            finally
            {
                fixture.Claw.TargetResidencyChanged -= Observe;
                if (route == ResetRoute.ExitTree && GodotObject.IsInstanceValid(fixture.Claw))
                    fixture.Claw.QueueFree();
                await Dispose(tree, fixture.Root);
            }

            void Observe(GrappleSurface2D target, bool resident)
            {
                if (resident) pins++;
                else
                {
                    unpins++;
                    TestAssert.Equal(target, fixture.Claw.ResidencyTarget,
                        $"{route} keeps the old target readable during unpin");
                }
            }
        }
    }

    private static async Task NotifyTargetWillUnloadUnpinsThenClearsSynchronously(SceneTree tree)
    {
        Fixture fixture = await CreateFixture(tree, "ForcedUnload");
        var order = new List<string>();
        try
        {
            AdvanceToAttached(fixture);
            fixture.Claw.TargetResidencyChanged += ObserveResidency;
            fixture.Claw.ClawFrameChanged += ObserveFrame;

            bool notified = fixture.Claw.NotifyTargetWillUnload(fixture.Surface.GetParent());

            TestAssert.True(notified && order.Count >= 3 && order[0] == "unpin" &&
                order[1] == "Failed" && order[^1] == "Idle",
                "forced unload synchronously unpins before Failed and terminal Idle publication");
            TestAssert.True(fixture.Claw.CurrentFrame.State == MechanicalClawState.Idle &&
                fixture.Claw.ResidencyTarget == null && fixture.Claw.CurrentTarget == null,
                "forced unload returns with Idle and no target references");
            TestAssert.Near(1f, fixture.Motor.GrappleMovementScale, .0001f,
                "forced unload returns with normal motor scale");
        }
        finally
        {
            fixture.Claw.TargetResidencyChanged -= ObserveResidency;
            fixture.Claw.ClawFrameChanged -= ObserveFrame;
            await Dispose(tree, fixture.Root);
        }

        void ObserveResidency(GrappleSurface2D target, bool resident)
        {
            if (resident) return;
            TestAssert.True(GodotObject.IsInstanceValid(target) &&
                ReferenceEquals(fixture.Claw.ResidencyTarget, target) &&
                ReferenceEquals(fixture.Claw.CurrentTarget, target),
                "forced-unload unpin sees the valid old target before either reference clears");
            order.Add("unpin");
        }

        void ObserveFrame(MechanicalClawFrame frame, GrappleSurface2D target)
        {
            if (frame.State is MechanicalClawState.Failed or MechanicalClawState.Idle)
                order.Add(frame.State.ToString());
        }
    }

    private static async Task SubscriberCanReadResidencyTargetAfterLateBinding(SceneTree tree)
    {
        Fixture fixture = await CreateFixture(tree, "LateSubscriber");
        var observed = new List<(GrappleSurface2D Target, bool Resident)>();
        try
        {
            AdvanceToTravel(fixture);

            // Streamer consumer order is binding: subscribe first, then immediately read the
            // current target to acquire an already-active sector before handling future events.
            fixture.Claw.TargetResidencyChanged += Observe;
            GrappleSurface2D alreadyActive = fixture.Claw.ResidencyTarget;
            if (alreadyActive != null) observed.Add((alreadyActive, true));

            Step(fixture, Input(primaryPressed: true, mouseWorld: fixture.AimWorld));
            fixture.Claw.TargetResidencyChanged -= Observe;
            TestAssert.True(observed.Count == 2 && observed[0] == (fixture.Surface, true) &&
                observed[1] == (fixture.Surface, false),
                "a late subscriber acquires the active target by read, then receives future unpin");
        }
        finally { await Dispose(tree, fixture.Root); }

        void Observe(GrappleSurface2D target, bool resident) => observed.Add((target, resident));
    }

    private static async Task ThrowingSubscriberCannotPreventCleanupOrOtherSubscribers(SceneTree tree)
    {
        Fixture fixture = await CreateFixture(tree, "ThrowingSubscriber");
        var later = new List<bool>();
        try
        {
            fixture.Claw.TargetResidencyChanged += Throwing;
            fixture.Claw.TargetResidencyChanged += Later;
            AdvanceToTravel(fixture);
            Step(fixture, Input(primaryPressed: true, mouseWorld: fixture.AimWorld));

            TestAssert.True(later.Count == 2 && later[0] && !later[1],
                "a later subscriber receives both transitions despite an earlier throwing observer");
            TestAssert.True(fixture.Claw.ResidencyTarget == null && fixture.Claw.CurrentTarget == null,
                "a throwing observer cannot prevent target cleanup");
        }
        finally
        {
            fixture.Claw.TargetResidencyChanged -= Throwing;
            fixture.Claw.TargetResidencyChanged -= Later;
            await Dispose(tree, fixture.Root);
        }

        static void Throwing(GrappleSurface2D target, bool resident) =>
            throw new InvalidOperationException("intentional residency observer failure");
        void Later(GrappleSurface2D target, bool resident) => later.Add(resident);
    }

    private static async Task ResetInsidePinOrUnpinCallbackDoesNotRecurseOrRepin(SceneTree tree)
    {
        Fixture pinReset = await CreateFixture(tree, "ResetInsidePin");
        try
        {
            var events = new List<bool>();
            int depth = 0;
            int maximumDepth = 0;
            pinReset.Claw.TargetResidencyChanged += Observe;
            Step(pinReset, Input(grappleModePressed: true, primaryPressed: true,
                primaryHeld: true, mouseWorld: pinReset.AimWorld));
            for (int frame = 0; frame < 30 && events.Count == 0; frame++)
                Step(pinReset, Input(primaryHeld: true, mouseWorld: pinReset.AimWorld));
            pinReset.Claw.TargetResidencyChanged -= Observe;
            AssertResetTerminal(pinReset, events, maximumDepth, "pin callback reset");

            void Observe(GrappleSurface2D target, bool resident)
            {
                depth++;
                maximumDepth = Math.Max(maximumDepth, depth);
                events.Add(resident);
                if (resident) pinReset.Claw.ResetTransientState();
                depth--;
            }
        }
        finally { await Dispose(tree, pinReset.Root); }

        Fixture unpinReset = await CreateFixture(tree, "ResetInsideUnpin");
        try
        {
            var events = new List<bool>();
            int depth = 0;
            int maximumDepth = 0;
            unpinReset.Claw.TargetResidencyChanged += Observe;
            AdvanceToAttached(unpinReset);
            Step(unpinReset, Input(jumpPressed: true, mouseWorld: unpinReset.AimWorld));
            unpinReset.Claw.TargetResidencyChanged -= Observe;
            AssertResetTerminal(unpinReset, events, maximumDepth, "unpin callback reset");

            void Observe(GrappleSurface2D target, bool resident)
            {
                depth++;
                maximumDepth = Math.Max(maximumDepth, depth);
                events.Add(resident);
                if (!resident) unpinReset.Claw.ResetTransientState();
                depth--;
            }
        }
        finally { await Dispose(tree, unpinReset.Root); }

        Fixture forcedLossReset = await CreateFixture(tree, "ResetInsideForcedLossUnpin");
        try
        {
            var events = new List<bool>();
            var frames = new List<MechanicalClawFrame>();
            int depth = 0;
            int maximumDepth = 0;
            forcedLossReset.Claw.TargetResidencyChanged += ObserveResidency;
            forcedLossReset.Claw.ClawFrameChanged += ObserveFrame;
            AdvanceToAttached(forcedLossReset);

            bool notified = forcedLossReset.Claw.NotifyTargetWillUnload(forcedLossReset.Surface);

            forcedLossReset.Claw.TargetResidencyChanged -= ObserveResidency;
            forcedLossReset.Claw.ClawFrameChanged -= ObserveFrame;
            TestAssert.True(notified,
                "the forced-loss callback reset fixture invalidates the real active target");
            AssertResetTerminal(forcedLossReset, events, maximumDepth,
                "forced-loss unpin callback reset");
            TestAssert.True(!frames.Exists(frame => frame.State == MechanicalClawState.Failed),
                "forced-loss cleanup does not publish stale Failed after its unpin callback reset");

            void ObserveResidency(GrappleSurface2D target, bool resident)
            {
                depth++;
                maximumDepth = Math.Max(maximumDepth, depth);
                events.Add(resident);
                if (!resident)
                {
                    TestAssert.True(GodotObject.IsInstanceValid(target) &&
                        ReferenceEquals(forcedLossReset.Claw.ResidencyTarget, target) &&
                        ReferenceEquals(forcedLossReset.Claw.CurrentTarget, target),
                        "forced-loss callback reset still sees both valid target references");
                    forcedLossReset.Claw.ResetTransientState();
                }
                depth--;
            }

            void ObserveFrame(MechanicalClawFrame frame, GrappleSurface2D target) => frames.Add(frame);
        }
        finally { await Dispose(tree, forcedLossReset.Root); }
    }

    private static void AssertResetTerminal(Fixture fixture, List<bool> events,
        int maximumDepth, string context)
    {
        TestAssert.True(events.Count == 2 && events[0] && !events[1],
            $"{context} publishes one true/false pair without stale repin");
        TestAssert.Equal(1, maximumDepth,
            $"{context} queues residency changes instead of recursively invoking observers");
        TestAssert.True(fixture.Claw.CurrentFrame.State == MechanicalClawState.Idle &&
            fixture.Claw.CurrentFrame.VelocityCorrection.IsZeroApprox() &&
            fixture.Claw.ResidencyTarget == null && fixture.Claw.CurrentTarget == null,
            $"{context} ends Idle with no target or correction");
        TestAssert.Near(1f, fixture.Motor.GrappleMovementScale, .0001f,
            $"{context} cannot reapply a stale motor constraint");
    }

    private static async Task<Fixture> CreateFixture(SceneTree tree, string name,
        bool physicalTerrain = false, bool legacy = false)
    {
        var root = new Node2D { Name = "ClawResidency" + name };
        GrappleSurface2D surface;
        Vector2 aimWorld;
        if (physicalTerrain)
        {
            var terrain = new StaticBody2D
            {
                Name = "Terrain",
                Position = new Vector2(300f, 0f),
                CollisionLayer = CombatCollisionLayers.World | CombatCollisionLayers.ThreadTarget,
                CollisionMask = CombatCollisionLayers.None
            };
            terrain.AddChild(new CollisionShape2D
            {
                Name = "Collision",
                Shape = new RectangleShape2D { Size = new Vector2(20f, 180f) }
            });
            surface = new GrappleSurface2D
            {
                Name = "GrappleSurfaceProfile",
                Material = GrappleSurfaceMaterial.GripStone,
                UseCollisionHitPoint = true
            };
            terrain.AddChild(surface);
            root.AddChild(terrain);
            aimWorld = new Vector2(500f, 0f);
        }
        else
        {
            surface = legacy
                ? new ThreadTarget2D { Name = "Surface", Kind = ThreadTargetKind.Mechanism }
                : new GrappleSurface2D { Name = "Surface" };
            surface.Position = new Vector2(300f, 0f);
            surface.Material = GrappleSurfaceMaterial.DedicatedAnchor;
            surface.AssistRadius = MechanicalClawSettings.Default.MainAnchorAssistRadius;
            var area = new Area2D
            {
                Name = "TargetCollision",
                CollisionLayer = CombatCollisionLayers.ThreadTarget,
                CollisionMask = CombatCollisionLayers.None,
                Monitoring = true,
                Monitorable = true
            };
            area.AddChild(new CollisionShape2D
            {
                Name = "Collision",
                Shape = new CircleShape2D { Radius = 12f }
            });
            surface.AddChild(area);
            if (legacy)
                surface.AddChild(new LegacyThreadInteractionAdapter
                    { Name = "LegacyThreadInteractionAdapter" });
            root.AddChild(surface);
            aimWorld = surface.Position;
        }

        CharacterBody2D player = GD.Load<PackedScene>("res://scenes/player/Player.tscn")
            .Instantiate<CharacterBody2D>();
        player.Name = "Player";
        player.GetNode<Camera2D>("Camera2D").Enabled = false;
        root.AddChild(player);
        tree.Root.AddChild(root);
        await Frames(tree, 2);

        var fixture = new Fixture(root, player, surface, aimWorld,
            player.GetNode<PlayerInputReader>("Input"),
            player.GetNode<PlayerActionModeController>("ActionModes"),
            player.GetNode<MechanicalClawController>("MechanicalClaw"),
            player.GetNode<PlayerMotor>("Motor"),
            player.GetNode<HealthComponent>("Health"));
        fixture.Input.SetPhysicsProcess(false);
        fixture.Modes.SetPhysicsProcess(false);
        fixture.Claw.SetPhysicsProcess(false);
        fixture.Motor.SetPhysicsProcess(false);
        fixture.Health.SetPhysicsProcess(false);
        player.GetNode<PlayerShieldController>("Shield").SetPhysicsProcess(false);
        player.GetNode<PlayerEnergyController>("Energy").SetPhysicsProcess(false);
        player.GetNode<PlayerCombat>("Combat").SetPhysicsProcess(false);
        return fixture;
    }

    private static void AdvanceToTravel(Fixture fixture)
    {
        Step(fixture, Input(grappleModePressed: true, primaryPressed: true,
            primaryHeld: true, mouseWorld: fixture.AimWorld));
        for (int frame = 0; frame < 30 && fixture.Claw.CurrentFrame.State != MechanicalClawState.Travel; frame++)
            Step(fixture, Input(primaryHeld: true, mouseWorld: fixture.AimWorld));
        TestAssert.Equal(MechanicalClawState.Travel, fixture.Claw.CurrentFrame.State,
            "the real controller reaches Travel inside the bounded fixture window");
    }

    private static void AdvanceToAttached(Fixture fixture)
    {
        AdvanceToTravel(fixture);
        for (int frame = 0; frame < 120 && !IsAttached(fixture.Claw.CurrentFrame.State); frame++)
            Step(fixture, Input(mouseWorld: fixture.AimWorld));
        TestAssert.True(IsAttached(fixture.Claw.CurrentFrame.State),
            "the real controller reaches an attached phase inside the bounded fixture window");
    }

    private static void Step(Fixture fixture, PlayerInputFrame frame)
    {
        typeof(PlayerInputReader).GetProperty(nameof(PlayerInputReader.CurrentFrame),
            BindingFlags.Instance | BindingFlags.Public)!.SetValue(fixture.Input, frame);
        fixture.Modes._PhysicsProcess(Frame);
        fixture.Claw._PhysicsProcess(Frame);
    }

    private static PlayerInputFrame Input(bool grappleModePressed = false,
        bool attackModePressed = false, bool primaryPressed = false, bool primaryHeld = false,
        bool shieldHeld = false, bool jumpPressed = false, bool dashPressed = false,
        Vector2 mouseWorld = default) =>
        new(0f, Vector2.Zero, jumpPressed, false, dashPressed, false, false,
            MouseWorld: mouseWorld,
            AttackModePressed: attackModePressed,
            GrappleModePressed: grappleModePressed,
            PrimaryPressed: primaryPressed,
            PrimaryHeld: primaryHeld,
            ShieldHeld: shieldHeld);

    private static bool IsAttached(MechanicalClawState state) => state is
        MechanicalClawState.Latched or MechanicalClawState.Reel or MechanicalClawState.Swing;

    private static async Task Dispose(SceneTree tree, Node root)
    {
        if (GodotObject.IsInstanceValid(root)) root.QueueFree();
        await Frames(tree, 2);
    }

    private static async Task Frames(SceneTree tree, int count)
    {
        for (int index = 0; index < count; index++)
            await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
    }

    private enum ReleaseRoute { Cancel, Boost, Dash, Shield, Q }
    private enum ResetRoute { Damage, Death, Reset, ExitTree }

    private sealed record Fixture(Node2D Root, CharacterBody2D Player,
        GrappleSurface2D Surface, Vector2 AimWorld, PlayerInputReader Input,
        PlayerActionModeController Modes, MechanicalClawController Claw,
        PlayerMotor Motor, HealthComponent Health);
}
