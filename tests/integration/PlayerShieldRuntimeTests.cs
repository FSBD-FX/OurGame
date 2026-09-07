using System;
using System.Threading.Tasks;
using Godot;
using Knight.Combat;
using Knight.Player;
using Knight.Presentation;
using Knight.Shared;

namespace Knight.Tests.Integration;

public static class PlayerShieldRuntimeTests
{
    public static async Task Run(SceneTree tree)
    {
        Fixture fixture = await CreateFixture(tree);
        try
        {
            await PositionedHitboxesResolveFromAttackerSide(tree, fixture);
            await FrontNormalIsConsumedAndDuplicateIdentityIsIgnored(tree, fixture);
            await RearNormalPassesThrough(tree, fixture);
            await FrontHeavyWithTwentyFourEnergyResolvesOnce(tree, fixture);
            await FrontHeavyWithRemainingEnergyKeepsShieldSustained(tree, fixture);
            await FrontHeavyWithoutEnergyBreaksGuard(tree, fixture);
            await ShieldMovementComposesWithAbilityMovement(tree, fixture);
            await OverheatClosesShieldInTheSamePhysicsFrame(tree, fixture);
            await DeathAndResetCloseShieldImmediately(tree, fixture);
            VerifyShieldVisualIsPresentationOnly(fixture);
        }
        finally
        {
            Input.ActionRelease(InputActions.Shield);
            fixture.Root.QueueFree();
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        }
    }

    private static async Task PositionedHitboxesResolveFromAttackerSide(SceneTree tree, Fixture fixture)
    {
        await ActivateAtFullEnergy(tree, fixture);
        int before = fixture.Health.CurrentHealth;

        Hitbox front = await AddAttackerHitbox(tree, fixture, "FrontShieldAttacker", Vector2.Right, 110UL);
        TestAssert.True(front.TryConnect(fixture.Hurtbox),
            "a real Hitbox on the aimed front side connects to the player Hurtbox");
        TestAssert.Equal(before, fixture.Health.CurrentHealth,
            "the front-side Hitbox is guarded even though its knockback points attacker-to-player");

        Hitbox rear = await AddAttackerHitbox(tree, fixture, "RearShieldAttacker", Vector2.Left, 111UL);
        TestAssert.True(rear.TryConnect(fixture.Hurtbox),
            "a mirrored real Hitbox connects with a distinct attack identity");
        TestAssert.Equal(before - 1, fixture.Health.CurrentHealth,
            "the rear-side Hitbox bypasses the directional shield");
    }

    private static async Task FrontNormalIsConsumedAndDuplicateIdentityIsIgnored(SceneTree tree, Fixture fixture)
    {
        await ActivateAtFullEnergy(tree, fixture);
        int before = fixture.Health.CurrentHealth;
        var request = new DamageRequest(1, Vector2.Left, 100f, 100UL, 900UL, DamageGuardClass.Normal);

        TestAssert.True(fixture.Hurtbox.Receive(request), "front normal is consumed by the guarded receiver");
        TestAssert.True(!fixture.Hurtbox.Receive(request),
            "fully guarded attack identity is consumed exactly once without reaching health");
        TestAssert.True(fixture.Hurtbox.Receive(request with { SourceRuntimeId = 901UL }),
            "the same local ordinal from a different runtime source remains a distinct guarded attack");
        TestAssert.Equal(before, fixture.Health.CurrentHealth, "front normal leaves health unchanged");
    }

    private static async Task RearNormalPassesThrough(SceneTree tree, Fixture fixture)
    {
        await ActivateAtFullEnergy(tree, fixture);
        int before = fixture.Health.CurrentHealth;

        TestAssert.True(fixture.Hurtbox.Receive(new DamageRequest(
            1, Vector2.Right, 100f, 101UL, 900UL, DamageGuardClass.Normal)),
            "rear normal reaches health through the active directional shield");
        TestAssert.Equal(before - 1, fixture.Health.CurrentHealth, "rear normal retains full damage");
    }

    private static async Task FrontHeavyWithTwentyFourEnergyResolvesOnce(SceneTree tree, Fixture fixture)
    {
        fixture.Health.ResetFull();
        fixture.Energy.ResetFull();
        fixture.Shield.ResetFull();
        fixture.Energy.TrySpendHeavyGuard(76f);
        await ActivateWithoutDrain(fixture);
        int before = fixture.Health.CurrentHealth;

        var request = new DamageRequest(
            3, Vector2.Left, 100f, 102UL, 900UL, DamageGuardClass.Heavy);
        TestAssert.True(fixture.Hurtbox.Receive(request),
            "front heavy with exactly 24 energy delegates one resolved hit to health");
        TestAssert.True(!fixture.Hurtbox.Receive(request),
            "a guarded heavy delegates its resolved identity to health only once");
        TestAssert.Equal(before - 1, fixture.Health.CurrentHealth,
            "front heavy resolves to exactly one damage rather than its original amount");
        TestAssert.Near(0f, fixture.Energy.EnergyRatio, .001f, "front heavy spends exactly the remaining 24 energy");
        TestAssert.True(!fixture.Shield.IsActive, "the zero-energy heavy closes the shield in that same frame");
    }

    private static async Task FrontHeavyWithRemainingEnergyKeepsShieldSustained(SceneTree tree, Fixture fixture)
    {
        fixture.Health.ResetFull();
        fixture.Energy.ResetFull();
        fixture.Shield.ResetFull();
        await ActivateWithoutDrain(fixture);
        int before = fixture.Health.CurrentHealth;

        TestAssert.True(fixture.Hurtbox.Receive(new DamageRequest(
            3, Vector2.Left, 100f, 105UL, 900UL, DamageGuardClass.Heavy)),
            "a full-energy front heavy resolves through the guarded receiver");
        TestAssert.Near(.76f, fixture.Energy.EnergyRatio, .001f,
            "an affordable heavy spends exactly 24 energy from 100");
        TestAssert.True(fixture.Energy.IsShieldSustained && fixture.Shield.IsActive,
            "100-to-76 heavy spending preserves active shield sustain in the same physics frame");

        TestAssert.True(fixture.Hurtbox.Receive(new DamageRequest(
            3, Vector2.Left, 100f, 106UL, 900UL, DamageGuardClass.Heavy)),
            "a second distinct same-frame front heavy still resolves through the active shield");
        TestAssert.Equal(before - 2, fixture.Health.CurrentHealth,
            "both affordable heavy requests resolve to one damage each");
        TestAssert.True(fixture.Shield.IsActive,
            "remaining energy leaves the shield active after the second resolved heavy");
    }

    private static async Task FrontHeavyWithoutEnergyBreaksGuard(SceneTree tree, Fixture fixture)
    {
        fixture.Health.ResetFull();
        fixture.Energy.ResetFull();
        fixture.Shield.ResetFull();
        fixture.Energy.TrySpendHeavyGuard(80f);
        await ActivateWithoutDrain(fixture);
        int before = fixture.Health.CurrentHealth;

        TestAssert.True(fixture.Hurtbox.Receive(new DamageRequest(
            3, Vector2.Left, 100f, 103UL, 900UL, DamageGuardClass.Heavy)),
            "front heavy without 24 energy reaches health after guard break");
        TestAssert.Equal(before - 3, fixture.Health.CurrentHealth,
            "insufficient-energy heavy retains its original full damage");
        TestAssert.True(!fixture.Shield.IsActive, "insufficient-energy heavy breaks the shield immediately");
    }

    private static async Task ShieldMovementComposesWithAbilityMovement(SceneTree tree, Fixture fixture)
    {
        fixture.Health.ResetFull();
        fixture.Energy.ResetFull();
        fixture.Shield.ResetFull();
        Input.ActionRelease(InputActions.Shield);
        fixture.Shield._PhysicsProcess(0d);
        fixture.Motor.AbilityMovementScale = .5f;

        fixture.Shield._PhysicsProcess(0d);
        TestAssert.Near(.5f, fixture.Motor.AbilityMovementScale, .001f,
            "an inactive shield does not erase ThreadAbility's movement scale");
        TestAssert.Near(1f, fixture.Motor.ShieldMovementScale, .001f,
            "an inactive shield contributes its neutral movement modifier");
        TestAssert.Near(.5f, fixture.Motor.EffectiveMovementScale, .001f,
            "the motor preserves the ability movement modifier while shield is inactive");

        await ActivateWithoutDrain(fixture);
        TestAssert.Near(.5f, fixture.Motor.AbilityMovementScale, .001f,
            "an active shield leaves ThreadAbility's movement modifier under its original owner");
        TestAssert.Near(.65f, fixture.Motor.ShieldMovementScale, .001f,
            "the active shield writes only its dedicated movement modifier");
        TestAssert.Near(.325f, fixture.Motor.EffectiveMovementScale, .001f,
            "the motor composes ThreadAbility and shield movement modifiers multiplicatively");
    }

    private static async Task OverheatClosesShieldInTheSamePhysicsFrame(SceneTree tree, Fixture fixture)
    {
        fixture.Health.ResetFull();
        fixture.Energy.ResetFull();
        fixture.Shield.ResetFull();
        await ActivateAtFullEnergy(tree, fixture);

        fixture.Energy._PhysicsProcess(100d / 42d);
        fixture.Shield._PhysicsProcess(0d);

        TestAssert.True(fixture.Energy.IsOverheated && !fixture.Energy.IsShieldSustained,
            "energy controller closes sustain in the physics step that reaches full heat");
        TestAssert.True(!fixture.Shield.IsActive, "overheat removes authoritative shield state in that same frame");
    }

    private static async Task DeathAndResetCloseShieldImmediately(SceneTree tree, Fixture fixture)
    {
        fixture.Health.ResetFull();
        fixture.Energy.ResetFull();
        fixture.Shield.ResetFull();
        await ActivateAtFullEnergy(tree, fixture);

        TestAssert.True(fixture.Health.ApplyDamage(new DamageRequest(
            99, Vector2.Right, 0f, 104UL, 900UL)), "fatal fixture damage is accepted");
        TestAssert.True(!fixture.Shield.IsActive, "death signal removes shield state immediately");

        fixture.Health.ResetFull();
        fixture.Shield.ResetFull();
        TestAssert.True(!fixture.Shield.IsActive, "reset leaves shield closed until shield is requested again");
    }

    private static void VerifyShieldVisualIsPresentationOnly(Fixture fixture)
    {
        Node shieldVisual = fixture.Player.GetNode("VisualRoot/Shield");
        TestAssert.True(shieldVisual is PlayerShieldPlaceholderVisual && shieldVisual is not CollisionObject2D,
            "VisualRoot/Shield is a presentation-only shield fan without collision authority");
    }

    private static async Task<Fixture> CreateFixture(SceneTree tree)
    {
        Input.ActionRelease(InputActions.Shield);
        var root = new Node2D { Name = "PlayerShieldRuntimeFixture" };
        CharacterBody2D player = GD.Load<PackedScene>("res://scenes/player/Player.tscn").Instantiate<CharacterBody2D>();
        player.GetNode<Camera2D>("Camera2D").Enabled = false;
        root.AddChild(player);
        tree.Root.AddChild(root);
        await Frames(tree, 2);
        PlayerInputReader input = player.GetNode<PlayerInputReader>("Input");
        PlayerMotor motor = player.GetNode<PlayerMotor>("Motor");
        motor.SetPhysicsProcess(false);
        player.GlobalPosition = input.Current.MouseWorld - Vector2.Right * 100f;
        await Frames(tree, 1);
        HealthComponent health = player.GetNode<HealthComponent>("Health");
        health.InvulnerabilitySeconds = 0f;
        return new Fixture(root, player, health, player.GetNode<Hurtbox>("Hurtbox"),
            player.GetNode<PlayerEnergyController>("Energy"), player.GetNode<PlayerShieldController>("Shield"), motor);
    }

    private static async Task<Hitbox> AddAttackerHitbox(SceneTree tree, Fixture fixture, string name,
        Vector2 playerToAttacker, ulong attackId)
    {
        var attacker = new Node2D
        {
            Name = name,
            GlobalPosition = fixture.Player.GlobalPosition + playerToAttacker * 100f
        };
        var combat = new Node2D { Name = "Combat" };
        var hitbox = new Hitbox
        {
            Name = "Hitbox",
            Damage = 1,
            Knockback = 100f,
            GuardClass = DamageGuardClass.Normal
        };
        attacker.AddChild(combat);
        combat.AddChild(hitbox);
        fixture.Root.AddChild(attacker);
        await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
        hitbox.Begin(attackId);
        return hitbox;
    }

    private static async Task ActivateAtFullEnergy(SceneTree tree, Fixture fixture)
    {
        fixture.Energy.ResetFull();
        fixture.Shield.ResetFull();
        Input.ActionPress(InputActions.Shield);
        await Frames(tree, 1);
        TestAssert.True(fixture.Shield.IsActive, "right mouse shield input activates a sustained shield");
    }

    private static async Task ActivateWithoutDrain(Fixture fixture)
    {
        Input.ActionPress(InputActions.Shield);
        fixture.Shield._PhysicsProcess(0d);
        fixture.Energy._PhysicsProcess(0d);
        fixture.Shield._PhysicsProcess(0d);
        await Task.CompletedTask;
        TestAssert.True(fixture.Shield.IsActive, "zero-delta runtime frame exposes the shield before an exact-energy hit");
    }

    private static async Task Frames(SceneTree tree, int count)
    {
        for (int index = 0; index < count; index++)
            await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
    }

    private sealed record Fixture(
        Node2D Root,
        CharacterBody2D Player,
        HealthComponent Health,
        Hurtbox Hurtbox,
        PlayerEnergyController Energy,
        PlayerShieldController Shield,
        PlayerMotor Motor);
}
