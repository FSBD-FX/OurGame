using System.Threading.Tasks;
using Godot;
using Knight.Combat;
using Knight.Enemies.Ranged;
using Knight.Player;

namespace Knight.Tests.Unit;

public static class RangedVolleyDamageLedgerTests
{
    // Removing the ledger or keying by individual pellet/Hurtbox would let a
    // live volley hit again after HP invulnerability or its 64-ID cache expires.
    public static async Task Run(SceneTree tree)
    {
        var owner = new Node2D { Name = "RangedLedgerRealHealthFixture" };
        var health = new HealthComponent { Name = "Health", MaxHealth = 100, InvulnerabilitySeconds = 0f };
        var first = new Hurtbox { Name = "BodyA", DamageReceiverPath = new NodePath("../Health") };
        var second = new Hurtbox { Name = "BodyB", DamageReceiverPath = new NodePath("../Health") };
        owner.AddChild(health);
        owner.AddChild(first);
        owner.AddChild(second);
        tree.Root.AddChild(owner);
        try
        {
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            var ledger = new RangedVolleyDamageLedger();
            DamageRequest shot = Request(7, 1);
            TestAssert.True(ledger.TryDeliver(first, shot) && health.CurrentHealth == 99,
                "RANGED_LEDGER_FIRST_DELIVERY_RED actual Hurtbox receives one volley damage");
            TestAssert.True(!ledger.TryDeliver(first, shot) && !ledger.TryDeliver(second, shot) &&
                            health.CurrentHealth == 99,
                "one volley cannot multiply damage across pellets or two Hurtboxes on the same receiver");
            // Reset the receiver cache alone; a still-live volley must remain consumed.
            health.ResetFull();
            TestAssert.True(!ledger.TryDeliver(second, shot) && health.CurrentHealth == 100,
                "the ledger, not only HealthComponent attack caching, rejects the live repeated volley");

            TestAssert.True(ledger.TryDeliver(first, Request(7, 2)) &&
                            ledger.TryDeliver(first, Request(8, 1)) && health.CurrentHealth == 98,
                "burst round ordinals and different source actors remain independent damage attempts");

            ledger.Clear();
            health.ResetFull();
            health.InvulnerabilitySeconds = 1f;
            TestAssert.True(health.ReceiveDamage(Request(99, 99)), "the real health invulnerability starts");
            DamageRequest rejected = Request(7, 3);
            TestAssert.True(!ledger.TryDeliver(first, rejected), "an invulnerable target rejects damage");
            DamageRequest otherRejected = Request(7, 4);
            TestAssert.True(!ledger.TryDeliver(second, otherRejected), "a separate live volley is also rejected");
            health._PhysicsProcess(2d);
            TestAssert.True(!ledger.TryDeliver(second, rejected) && health.CurrentHealth == 99,
                "a rejected attempt stays consumed after invulnerability expires, including another Hurtbox");
            ledger.Release(rejected.AttackIdentity);
            TestAssert.True(ledger.TryDeliver(second, rejected) && health.CurrentHealth == 98,
                "only explicit retirement release removes the live identity ledger entry");
            health._PhysicsProcess(2d);
            TestAssert.True(!ledger.TryDeliver(first, otherRejected) && health.CurrentHealth == 98,
                "releasing one retired volley does not unlock another still-live consumed identity");

            ledger.Clear();
            health.ResetFull();
            health.InvulnerabilitySeconds = 0f;
            TestAssert.True(ledger.TryDeliver(first, shot), "clear removes a prior owned volley record");
            for (ulong ordinal = 2; ordinal <= 70; ordinal++)
                TestAssert.True(health.ReceiveDamage(Request(7, ordinal)), "real receiver cache ages naturally");
            TestAssert.True(!ledger.TryDeliver(first, shot) && health.CurrentHealth == 30,
                "a long-lived volley stays consumed after the receiver's sixty-four-entry cache ages out");
            TestAssert.True(!ledger.TryDeliver(null, Request(7, 100)) &&
                            !ledger.TryDeliver(first, Request(0, 100)) &&
                            !ledger.TryDeliver(first, Request(7, 0)) &&
                            !ledger.TryDeliver(first, Request(7, 100) with { Amount = 0 }) &&
                            !ledger.TryDeliver(first, Request(7, 101) with { Direction = new Vector2(float.NaN, 0f) }) &&
                            !ledger.TryDeliver(first, Request(7, 102) with { Knockback = float.PositiveInfinity }) &&
                            health.CurrentHealth == 30,
                "invalid target/identity/amount cannot mutate real HP or authorize damage");
        }
        finally
        {
            owner.QueueFree();
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        }
        await ReceiverChangesRetainOneAuthoritativeTarget(tree);
        await ReentrantDamageCannotRepeatALiveVolley(tree);
        await ActualNormalShieldConsumesOneAttemptWithoutExtraHitCost(tree);
        GD.Print("RANGED_VOLLEY_LEDGER_OK actual_health=true rejection_consumed=true shared_receiver=true");
    }

    private static async Task ReceiverChangesRetainOneAuthoritativeTarget(SceneTree tree)
    {
        var owner = new Node2D { Name = "RangedLedgerReceiverRebinding" };
        var healthA = new HealthComponent { Name = "HealthA", MaxHealth = 10, InvulnerabilitySeconds = 0f };
        var healthB = new HealthComponent { Name = "HealthB", MaxHealth = 10, InvulnerabilitySeconds = 0f };
        var first = new Hurtbox { Name = "BodyA", DamageReceiverPath = new NodePath("../HealthA") };
        var second = new Hurtbox { Name = "BodyB", DamageReceiverPath = new NodePath("../HealthB") };
        owner.AddChild(healthA); owner.AddChild(healthB);
        owner.AddChild(first); owner.AddChild(second);
        tree.Root.AddChild(owner);
        try
        {
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            var ledger = new RangedVolleyDamageLedger();
            TestAssert.True(ledger.TryDeliver(first, Request(7, 1)) && healthA.CurrentHealth == 9,
                "the first receiver actually takes the initial shot");
            TestAssert.True(ledger.TryDeliver(second, Request(7, 1)) && healthB.CurrentHealth == 9,
                "one volley may hit two distinct real receivers once each");
            first.DamageReceiverPath = new NodePath("../HealthB");
            TestAssert.True(ledger.TryDeliver(first, Request(7, 2)) &&
                            healthA.CurrentHealth == 9 && healthB.CurrentHealth == 8,
                "RANGED_LEDGER_RECEIVER_CHANGE_RED accounting and actual damage follow the same rebound receiver");
            TestAssert.True(!ledger.TryDeliver(second, Request(7, 2)) && healthB.CurrentHealth == 8,
                "a second Hurtbox cannot repeat the rebound receiver's consumed volley");

            healthB.Name = "RetiredHealthB";
            healthB.Free();
            var replacement = new HealthComponent { Name = "HealthB", MaxHealth = 10, InvulnerabilitySeconds = 0f };
            owner.AddChild(replacement);
            TestAssert.True(ledger.TryDeliver(second, Request(7, 3)) && replacement.CurrentHealth == 9,
                "a same-path replacement is resolved live instead of calling the freed cached receiver");
            TestAssert.True(!ledger.TryDeliver(first, Request(7, 3)) && healthA.CurrentHealth == 9 &&
                            replacement.CurrentHealth == 9,
                "both Hurtboxes share the actual replacement receiver's volley identity");
            first.DamageReceiverPath = new NodePath("../Missing");
            TestAssert.True(!ledger.TryDeliver(first, Request(7, 4)),
                "a missing receiver rejects safely without authorizing another target");
        }
        finally
        {
            owner.QueueFree();
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        }
    }

    private static async Task ReentrantDamageCannotRepeatALiveVolley(SceneTree tree)
    {
        var owner = new Node2D { Name = "RangedLedgerReentry" };
        var health = new HealthComponent { Name = "Health", MaxHealth = 10, InvulnerabilitySeconds = 0f };
        var hurtbox = new Hurtbox { Name = "Body" };
        owner.AddChild(health); owner.AddChild(hurtbox); tree.Root.AddChild(owner);
        try
        {
            var ledger = new RangedVolleyDamageLedger();
            bool callbackRan = false, nestedAccepted = true;
            DamageRequest shot = Request(7, 1);
            health.Damaged += (_, _) =>
            {
                if (callbackRan) return;
                callbackRan = true;
                health.ResetFull(); // A synchronous reaction clears Health's own ID cache.
                nestedAccepted = ledger.TryDeliver(hurtbox, shot);
            };
            TestAssert.True(ledger.TryDeliver(hurtbox, shot) && callbackRan && !nestedAccepted &&
                            health.CurrentHealth == 10,
                "a synchronous damage callback cannot reenter the live volley after resetting receiver cache");
        }
        finally
        {
            owner.QueueFree();
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        }
    }

    private static async Task ActualNormalShieldConsumesOneAttemptWithoutExtraHitCost(SceneTree tree)
    {
        using PackedScene packed = GD.Load<PackedScene>("res://scenes/player/Player.tscn");
        var player = packed.Instantiate<CharacterBody2D>();
        player.GetNode<PlayerMotor>("Motor").SetPhysicsProcess(false);
        tree.Root.AddChild(player);
        try
        {
            player.GetNode<PlayerMotor>("Motor").SetPhysicsProcess(false);
            var shield = player.GetNode<PlayerShieldController>("Shield");
            var energy = player.GetNode<PlayerEnergyController>("Energy");
            var health = player.GetNode<HealthComponent>("Health");
            var target = player.GetNode<Hurtbox>("Hurtbox");
            var receiver = player.GetNode<PlayerGuardedDamageReceiver>("GuardedDamage");
            int shieldResults = 0;
            shield.ShieldResolved += (_, _) => shieldResults++;
            SetShieldKey(true);
            for (int frame = 0; frame < 12 && !shield.IsActive; frame++)
                await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
            TestAssert.True(shield.IsActive && shield.GuardDirection.X > .9f,
                "real keyboard L sustains a production right-facing shield before damage");
            float energyBefore = energy.EnergyRatio;
            var ledger = new RangedVolleyDamageLedger();
            DamageRequest shot = Request(7, 1) with { Direction = Vector2.Left };
            TestAssert.True(ledger.TryDeliver(target, shot) && !ledger.TryDeliver(target, shot) &&
                            health.CurrentHealth == 5 && shieldResults == 1 && energy.EnergyRatio == energyBefore,
                "a real normal block consumes one volley attempt without a separate per-pellet energy debit");
            SetShieldKey(false);
            for (int frame = 0; frame < 4; frame++)
                await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
            receiver.ResetFull();
            TestAssert.True(!shield.IsActive && !ledger.TryDeliver(target, shot) && health.CurrentHealth == 5,
                "a blocked live volley stays consumed after releasing guard and clearing receiver cache");
            TestAssert.True(ledger.TryDeliver(target, shot with { SourceAttackId = 2 }) && health.CurrentHealth == 4,
                "the next independent burst ordinal can hit after the real shield is released");
        }
        finally
        {
            SetShieldKey(false);
            player.QueueFree();
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        }
    }

    private static void SetShieldKey(bool pressed)
    {
        using var key = new InputEventKey { Keycode = Key.L, PhysicalKeycode = Key.L, Pressed = pressed };
        Input.ParseInputEvent(key);
        Input.FlushBufferedEvents();
    }

    private static DamageRequest Request(ulong source, ulong ordinal) =>
        new(1, Vector2.Right, 120f, ordinal, source, DamageGuardClass.Normal);
}
