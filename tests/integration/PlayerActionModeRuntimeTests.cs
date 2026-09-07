using System;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using Knight.Abilities;
using Knight.Player;

namespace Knight.Tests.Integration;

public static class PlayerActionModeRuntimeTests
{
    public static async Task Run(SceneTree tree)
    {
        Fixture fixture = await CreateFixture(tree);
        try
        {
            DefaultModeStartsAtAttackSlotOne(fixture);
            SelectionEventsObserveCommittedPublicState(fixture);
            EAndQStickTheSelectedMode(fixture);
            GrappleEligibilityAndAttackModeRequestHandoff(fixture);
            SlotFourStaysSelectedAndRejectsWithoutSpending(fixture);
            LowEnergyRejectsWithoutSpending(fixture);
            ActiveShieldRejectsWithoutSpending(fixture);
            AcceptedSlotOnePrimaryStartsCombatAndSpendsTwelve(fixture);
            FullEnergyRepeatPrimaryDoesNotSpendOrOverwriteFeedback(fixture);
        }
        finally
        {
            fixture.Root.QueueFree();
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        }
    }

    private static void DefaultModeStartsAtAttackSlotOne(Fixture fixture)
    {
        TestAssert.Equal(PlayerActionMode.Attack, fixture.Modes.CurrentMode,
            "generated player defaults the left-mouse router to Attack mode");
        TestAssert.Equal(1, fixture.Modes.ActiveSlot,
            "generated player defaults the left-mouse router to occupied slot one");
        TestAssert.True(fixture.Modes.CanBeginAttack,
            "default Attack slot one is eligible to begin melee at full energy");
    }

    private static void EAndQStickTheSelectedMode(Fixture fixture)
    {
        StepModes(fixture, Frame(grappleModePressed: true));
        TestAssert.Equal(PlayerActionMode.Grapple, fixture.Modes.CurrentMode,
            "E sticks Grapple mode after its input edge has passed");
        StepModes(fixture, Frame());
        TestAssert.Equal(PlayerActionMode.Grapple, fixture.Modes.CurrentMode,
            "Grapple selection persists without cursor-context switching");

        StepModes(fixture, Frame(attackModePressed: true));
        TestAssert.Equal(PlayerActionMode.Attack, fixture.Modes.CurrentMode,
            "Q sticks Attack mode after its input edge has passed");
    }

    private static void SelectionEventsObserveCommittedPublicState(Fixture fixture)
    {
        int modeCallbacks = 0;
        int loadoutCallbacks = 0;
        fixture.Modes.ModeChanged += mode =>
        {
            modeCallbacks++;
            TestAssert.Equal(mode, fixture.Modes.CurrentMode,
                "ModeChanged callbacks observe the committed CurrentMode payload");
            TestAssert.Equal(mode == PlayerActionMode.Attack && fixture.Modes.ActiveSlot == 1,
                fixture.Modes.CanBeginAttack,
                "ModeChanged callbacks observe eligibility from the committed selection state");
        };
        fixture.Modes.LoadoutChanged += slot =>
        {
            loadoutCallbacks++;
            TestAssert.Equal(slot, fixture.Modes.ActiveSlot,
                "LoadoutChanged callbacks observe the committed ActiveSlot payload");
            TestAssert.True(!fixture.Modes.CanBeginAttack,
                "slot-four callback observes the committed empty-slot attack rejection state");
        };

        StepModes(fixture, Frame(grappleModePressed: true));
        StepModes(fixture, Frame(attackModePressed: true));
        StepModes(fixture, Frame(requestedSlot: 4));

        TestAssert.Equal(2, modeCallbacks, "both sticky mode changes publish one committed callback");
        TestAssert.Equal(1, loadoutCallbacks, "slot selection publishes one committed callback");
    }

    private static void GrappleEligibilityAndAttackModeRequestHandoff(Fixture fixture)
    {
        fixture.Claw.ResetTransientState();
        fixture.Energy.ResetFull();
        fixture.Combat.ResetTransientState();
        StepModes(fixture, Frame(grappleModePressed: true, requestedSlot: 1));
        TestAssert.True(fixture.Modes.CanBeginGrapple,
            "Grapple mode is eligible before a right-mouse guard request begins");
        StepModes(fixture, Frame(shieldHeld: true));
        TestAssert.True(!fixture.Modes.CanBeginGrapple,
            "a right-mouse guard request blocks starting a new grapple");

        int attackModeRequests = 0;
        fixture.Modes.AttackModeRequested += () =>
        {
            attackModeRequests++;
            TestAssert.Equal(PlayerActionMode.Attack, fixture.Modes.CurrentMode,
                "AttackModeRequested callbacks observe the committed Q-selected Attack mode");
        };

        StepModes(fixture, Frame(attackModePressed: true));
        TestAssert.Equal(0, attackModeRequests,
            "Q emits no attack-mode request while compatible grapple activity is idle");

        StepModes(fixture, Frame(grappleModePressed: true));
        SetClawFrame(fixture.Claw, MechanicalClawFrame.Idle() with
        {
            CastId = 1UL,
            State = MechanicalClawState.Aim,
            EffectiveVisibility = true
        });
        TestAssert.Equal(MechanicalClawState.Aim, fixture.Claw.CurrentFrame.State,
            "the handoff fixture exposes compatible non-idle activity through the real mechanical claw");
        StepModes(fixture, Frame(attackModePressed: true));
        TestAssert.Equal(1, attackModeRequests,
            "Q emits exactly one attack-mode request while compatible grapple activity is active");
        fixture.Claw.ResetTransientState();
    }

    private static void SlotFourStaysSelectedAndRejectsWithoutSpending(Fixture fixture)
    {
        fixture.Energy.ResetFull();
        StepModes(fixture, Frame(requestedSlot: 4));
        StepModes(fixture, Frame());
        TestAssert.Equal(4, fixture.Modes.ActiveSlot,
            "slot four remains selected after the numeric-key edge has passed");

        float before = fixture.Energy.EnergyRatio;
        StepCombat(fixture, Frame(primaryPressed: true));
        TestAssert.True(!fixture.Combat.IsAttacking,
            "an empty selected slot never starts a melee attack from left mouse");
        TestAssert.Near(before, fixture.Energy.EnergyRatio, .0001f,
            "an empty selected slot spends no attack energy");
        TestAssert.Equal(PlayerActionRejectionReason.EmptySlot, fixture.Modes.LastRejection,
            "empty selected slots publish their distinct rejection reason");
    }

    private static void LowEnergyRejectsWithoutSpending(Fixture fixture)
    {
        fixture.Energy.ResetFull();
        fixture.Energy.TrySpendHeavyGuard(90f);
        StepModes(fixture, Frame(requestedSlot: 1));
        float before = fixture.Energy.EnergyRatio;
        StepCombat(fixture, Frame(primaryPressed: true));
        TestAssert.True(!fixture.Combat.IsAttacking,
            "insufficient energy never starts a melee attack from left mouse");
        TestAssert.Near(before, fixture.Energy.EnergyRatio, .0001f,
            "an energy-rejected melee request spends no energy");
        TestAssert.Equal(PlayerActionRejectionReason.InsufficientEnergy, fixture.Modes.LastRejection,
            "energy rejection is distinct from empty-slot rejection");
    }

    private static void ActiveShieldRejectsWithoutSpending(Fixture fixture)
    {
        fixture.Energy.ResetFull();
        SetInputFrame(fixture.Input, Frame(shieldHeld: true));
        fixture.Shield._PhysicsProcess(0d);
        fixture.Energy._PhysicsProcess(0d);
        fixture.Shield._PhysicsProcess(0d);
        TestAssert.True(fixture.Shield.IsActive, "fixture activates the real shield before routing primary input");

        float before = fixture.Energy.EnergyRatio;
        StepCombat(fixture, Frame(primaryPressed: true, shieldHeld: true));
        TestAssert.True(!fixture.Combat.IsAttacking,
            "an active shield rejects a new melee attack from left mouse");
        TestAssert.Near(before, fixture.Energy.EnergyRatio, .0001f,
            "a shield-rejected melee request spends no energy");
        TestAssert.Equal(PlayerActionRejectionReason.ShieldActive, fixture.Modes.LastRejection,
            "shield rejection is distinct from empty-slot and energy rejection");
        SetInputFrame(fixture.Input, Frame());
        fixture.Shield._PhysicsProcess(0d);
    }

    private static void AcceptedSlotOnePrimaryStartsCombatAndSpendsTwelve(Fixture fixture)
    {
        fixture.Combat.ResetTransientState();
        fixture.Energy.ResetFull();
        StepModes(fixture, Frame(attackModePressed: true, requestedSlot: 1));
        StepCombat(fixture, Frame(primaryPressed: true));

        TestAssert.True(fixture.Combat.IsAttacking,
            "Attack mode slot one routes accepted left mouse to the real combat model");
        TestAssert.Near(.88f, fixture.Energy.EnergyRatio, .001f,
            "an accepted slot-one melee transaction spends exactly 12 energy");
        TestAssert.Equal(PlayerActionRejectionReason.None, fixture.Modes.LastRejection,
            "a successful melee transaction clears the previous rejection state");
    }

    private static void FullEnergyRepeatPrimaryDoesNotSpendOrOverwriteFeedback(Fixture fixture)
    {
        fixture.Combat.ResetTransientState();
        fixture.Energy.ResetFull();
        StepModes(fixture, Frame(attackModePressed: true, requestedSlot: 1));
        StepCombat(fixture, Frame(primaryPressed: true));
        fixture.Energy.ResetFull();
        TestAssert.Equal(PlayerActionRejectionReason.None, fixture.Modes.LastRejection,
            "the first accepted attack starts with clear feedback before repeat-click coverage");

        StepCombat(fixture, Frame(primaryPressed: true));
        TestAssert.True(fixture.Combat.IsAttacking,
            "a busy full-energy repeat leaves the original real melee active");
        TestAssert.Near(1f, fixture.Energy.EnergyRatio, .0001f,
            "a busy full-energy repeat spends no attack energy");
        TestAssert.Equal(PlayerActionRejectionReason.None, fixture.Modes.LastRejection,
            "a busy full-energy repeat does not overwrite feedback as insufficient energy");
    }

    private static async Task<Fixture> CreateFixture(SceneTree tree)
    {
        var root = new Node2D { Name = "PlayerActionModeFixture" };
        CharacterBody2D player = GD.Load<PackedScene>("res://scenes/player/Player.tscn")
            .Instantiate<CharacterBody2D>();
        player.GetNode<Camera2D>("Camera2D").Enabled = false;
        root.AddChild(player);
        tree.Root.AddChild(root);
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);

        PlayerInputReader input = player.GetNode<PlayerInputReader>("Input");
        PlayerActionModeController modes = player.GetNode<PlayerActionModeController>("ActionModes");
        PlayerCombat combat = player.GetNode<PlayerCombat>("Combat");
        PlayerEnergyController energy = player.GetNode<PlayerEnergyController>("Energy");
        PlayerShieldController shield = player.GetNode<PlayerShieldController>("Shield");
        MechanicalClawController claw = player.GetNode<MechanicalClawController>("MechanicalClaw");
        input.SetPhysicsProcess(false);
        modes.SetPhysicsProcess(false);
        combat.SetPhysicsProcess(false);
        energy.SetPhysicsProcess(false);
        shield.SetPhysicsProcess(false);
        player.GetNode<PlayerMotor>("Motor").SetPhysicsProcess(false);
        claw.SetPhysicsProcess(false);
        return new Fixture(root, input, modes, combat, energy, shield, claw);
    }

    private static void StepModes(Fixture fixture, PlayerInputFrame frame)
    {
        SetInputFrame(fixture.Input, frame);
        fixture.Modes._PhysicsProcess(0d);
    }

    private static void StepCombat(Fixture fixture, PlayerInputFrame frame)
    {
        StepModes(fixture, frame);
        fixture.Combat._PhysicsProcess(0d);
    }

    private static PlayerInputFrame Frame(bool attackModePressed = false, bool grappleModePressed = false,
        int requestedSlot = 0, bool primaryPressed = false, bool shieldHeld = false) =>
        new(0f, Vector2.Right, false, false, false, false, false,
            AttackModePressed: attackModePressed,
            GrappleModePressed: grappleModePressed,
            RequestedSlot: requestedSlot,
            PrimaryPressed: primaryPressed,
            ShieldHeld: shieldHeld);

    private static void SetInputFrame(PlayerInputReader input, PlayerInputFrame frame)
    {
        typeof(PlayerInputReader).GetProperty(nameof(PlayerInputReader.CurrentFrame),
            BindingFlags.Instance | BindingFlags.Public)!.SetValue(input, frame);
    }

    private static void SetClawFrame(MechanicalClawController claw, MechanicalClawFrame frame)
    {
        typeof(MechanicalClawController).GetProperty(nameof(MechanicalClawController.CurrentFrame),
            BindingFlags.Instance | BindingFlags.Public)!.SetValue(claw, frame);
    }

    private sealed record Fixture(
        Node2D Root,
        PlayerInputReader Input,
        PlayerActionModeController Modes,
        PlayerCombat Combat,
        PlayerEnergyController Energy,
        PlayerShieldController Shield,
        MechanicalClawController Claw);
}
