using Godot;
using Knight.Abilities;
using Knight.Boss;
using Knight.Combat;
using Knight.Config;
using Knight.Presentation;
using Knight.Prologue;

namespace Knight.Tests.Unit;

public static class BossSceneStructureTests
{
    public static void Run()
    {
        TestAssert.True(ResourceLoader.Exists("res://scenes/boss/FallenBellkeeper.tscn"),
            "formal Fallen Bellkeeper scene exists");
        FallenBellkeeper boss = GD.Load<PackedScene>("res://scenes/boss/FallenBellkeeper.tscn")
            .Instantiate<FallenBellkeeper>();
        TestAssert.Equal(1, Count<CollisionShape2D>(boss, "BodyCollider"), "Boss has one named body collider");
        TestAssert.True(boss.GetNodeOrNull<HealthComponent>("Health") != null, "Boss has stable Health path");
        TestAssert.True(boss.GetNodeOrNull<Hurtbox>("Hurtbox") != null, "Boss has one Hurtbox");
        TestAssert.True(boss.GetNodeOrNull<Hitbox>("AttackHitbox") != null, "Boss has one attack hitbox");
        ThreadTarget2D chain = boss.GetNodeOrNull<ThreadTarget2D>("ChainTarget");
        TestAssert.True(chain != null && !chain.IsInGroup("thread_targets"),
            "chain target is inactive before the encounter starts");
        TestAssert.Equal(0f, chain.FallbackAimAssistRange,
            "Boss chain keeps precise directional aiming");
        TestAssert.True(boss.GetNodeOrNull<Node>("Presentation") != null &&
            boss.GetNodeOrNull<FallenBellkeeperFormalVisual>("VisualRoot/Formal") != null &&
            boss.GetNodeOrNull<Node2D>("VisualRoot/Effects") != null,
            "Boss exposes replaceable presentation paths");
        Node telegraphNode = boss.GetNodeOrNull("VisualRoot/AttackTelegraph");
        TestAssert.Equal(1, Count<AttackTelegraphVisual>(boss),
            "Boss owns exactly one recursive AttackTelegraphVisual");
        TestAssert.True(telegraphNode is AttackTelegraphVisual,
            "Boss exposes the scripted VisualRoot/AttackTelegraph path");
        TestAssert.True(telegraphNode is not CollisionObject2D && telegraphNode is not Control,
            "Boss telegraph cannot own collision layers, masks, or mouse input");
        Node2D telegraph = (Node2D)telegraphNode;
        Node2D effects = boss.GetNode<Node2D>("VisualRoot/Effects");
        TestAssert.Equal(6, telegraph.ZIndex,
            "Boss telegraph renders above the complete gameplay skin");
        TestAssert.Equal(10, effects.ZIndex,
            "Boss hit flashes remain above the attack telegraph");
        TestAssert.True(boss.GetNode<Node2D>("VisualRoot/Formal").ZIndex < telegraph.ZIndex &&
            telegraph.ZIndex < effects.ZIndex,
            "Boss visual stack is Formal then AttackTelegraph then Effects");
        TestAssert.Equal(12, boss.GetNode<HealthComponent>("Health").MaxHealth,
            "generated Boss health is the approved first-level value");
        TestAssert.Equal(1, boss.GetNode<Hitbox>("AttackHitbox").Damage,
            "generated Boss hitbox deals one first-level health cell");
        BossTuning tuning = boss.Tuning;
        AssertTiming(tuning.TimingFor(FallenBellkeeperAttack.Sweep), .80f, .25f, .70f, "Boss Sweep");
        AssertTiming(tuning.TimingFor(FallenBellkeeperAttack.LeapSlam), .90f, .25f, .75f, "Boss Leap Slam");
        AssertTiming(tuning.TimingFor(FallenBellkeeperAttack.GroundWave), 1.00f, .30f, .75f, "Boss Ground Wave");
        AssertTiming(tuning.TimingFor(FallenBellkeeperAttack.HammerThrow), .85f, .80f, .70f, "Boss Hammer Throw");
        TestAssert.Equal(1, tuning.ContactDamage,
            "generated Boss contact damage remains one first-level health cell");
        TestAssert.True(boss.GetNodeOrNull<Node2D>("VisualRoot/Formal/ArmorShell") != null &&
            boss.GetNodeOrNull<Node2D>("VisualRoot/Formal/Hammer") != null &&
            boss.GetNodeOrNull<Node2D>("VisualRoot/Formal/Chain") != null,
            "formal Boss exposes independent armor, hammer and chain geometry");
        boss.Free();

        TestAssert.True(ResourceLoader.Exists("res://scenes/prologue/rooms/FallenBellkeeperArena.tscn"),
            "formal Fallen Bellkeeper arena exists");
        PrologueRoom room = GD.Load<PackedScene>("res://scenes/prologue/rooms/FallenBellkeeperArena.tscn")
            .Instantiate<PrologueRoom>();
        TestAssert.Equal(0, room.ValidateContract("from_mechanism").Count,
            "arena accepts from_mechanism entrance");
        TestAssert.Equal(0, room.ValidateContract("boss_retry").Count,
            "arena accepts boss_retry entrance");
        TestAssert.True(room.GetNodeOrNull<BossArenaController>("BossArenaController") != null,
            "arena owns BossArenaController");
        TestAssert.True(room.GetNodeOrNull<CollisionShape2D>("Doors/Entry/Collision") != null &&
            room.GetNodeOrNull<CollisionShape2D>("Doors/Exit/Collision") != null,
            "arena owns entry and exit doors");
        TestAssert.True(room.GetNodeOrNull<CheckpointTrigger>("BossCheckpoint") != null,
            "arena owns a retry checkpoint");
        room.Free();
    }

    private static int Count<T>(Node root, string name = null) where T : Node
    {
        int count = root is T && (name == null || root.Name == name) ? 1 : 0;
        foreach (Node child in root.GetChildren()) count += Count<T>(child, name);
        return count;
    }

    private static void AssertTiming(BossAttackTiming actual, float telegraph, float active,
        float recovery, string label)
    {
        TestAssert.Equal(telegraph, actual.TelegraphSeconds, $"{label} preparation uses approved timing");
        TestAssert.Equal(active, actual.ActiveSeconds, $"{label} active window uses approved timing");
        TestAssert.Equal(recovery, actual.RecoverySeconds, $"{label} recovery uses approved timing");
    }
}
