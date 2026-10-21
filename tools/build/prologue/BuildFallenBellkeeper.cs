using Godot;
using Knight.Abilities;
using Knight.Boss;
using Knight.Combat;
using Knight.Config;
using Knight.Presentation;
using Knight.Tools;

namespace Knight.Build.Prologue;

public partial class BuildFallenBellkeeper : SceneTree
{
    public override void _Initialize()
    {
        var tuning = new BossTuning
        {
            MaxHealth = 12,
            SweepTelegraphSeconds = .80f,
            SweepActiveSeconds = .25f,
            SweepRecoverySeconds = .70f,
            LeapSlamTelegraphSeconds = .90f,
            LeapSlamActiveSeconds = .25f,
            LeapSlamRecoverySeconds = .75f,
            GroundWaveTelegraphSeconds = 1.00f,
            GroundWaveActiveSeconds = .30f,
            GroundWaveRecoverySeconds = .75f,
            HammerThrowTelegraphSeconds = .85f,
            HammerThrowActiveSeconds = .80f,
            HammerThrowRecoverySeconds = .70f,
            ContactDamage = 1
        };
        var boss = new FallenBellkeeper
        {
            Name = "FallenBellkeeper",
            CollisionLayer = CombatCollisionLayers.Enemy,
            CollisionMask = CombatCollisionLayers.World,
            Tuning = tuning
        };
        boss.AddChild(new HealthComponent
        {
            Name = "Health", MaxHealth = tuning.MaxHealth, InvulnerabilitySeconds = 0f
        });
        boss.AddChild(new ArmoredDamageReceiver
        {
            Name = "Armor", HealthPath = new NodePath("../Health"), Divisor = 3
        });
        var hurtbox = new Hurtbox
        {
            Name = "Hurtbox",
            CollisionLayer = CombatCollisionLayers.Enemy,
            CollisionMask = CombatCollisionLayers.None,
            Monitoring = true,
            Monitorable = true,
            DamageReceiverPath = new NodePath("../Health")
        };
        hurtbox.AddChild(new CollisionShape2D
        {
            Name = "HurtboxCollider",
            Shape = new CapsuleShape2D { Radius = 44f, Height = 126f }
        });
        boss.AddChild(hurtbox);
        var attack = new Hitbox
        {
            Name = "AttackHitbox",
            CollisionLayer = CombatCollisionLayers.EnemyAttack,
            CollisionMask = CombatCollisionLayers.Player,
            Damage = 1,
            Knockback = 360f,
            GuardClass = DamageGuardClass.Heavy,
            Monitoring = false,
            Monitorable = false
        };
        attack.AddChild(new CollisionShape2D
        {
            Name = "AttackCollider",
            Shape = new RectangleShape2D { Size = new Vector2(150, 54) }
        });
        boss.AddChild(attack);
        var chain = new ThreadTarget2D
        {
            Name = "ChainTarget",
            Position = new Vector2(70, -42),
            Kind = ThreadTargetKind.Mechanism,
            PullReceiverPath = new NodePath(".."),
            Material = GrappleSurfaceMaterial.TempleMetal,
            AssistRadius = MechanicalClawSettings.Default.MainAnchorAssistRadius
        };
        var targetArea = new Area2D
        {
            Name = "TargetCollision",
            CollisionLayer = CombatCollisionLayers.ThreadTarget,
            CollisionMask = CombatCollisionLayers.None,
            Monitoring = true
        };
        targetArea.AddChild(new CollisionShape2D
        {
            Name = "Collision", Disabled = true,
            Shape = new CircleShape2D { Radius = 32f }
        });
        chain.AddChild(targetArea);
        chain.AddChild(new LegacyThreadInteractionAdapter { Name = "LegacyThreadInteractionAdapter" });
        boss.AddChild(chain);
        boss.AddChild(new FallenBellkeeperPresentationCoordinator
        {
            Name = "Presentation", Profile = new PresentationProfile()
        });
        var visualRoot = new Node2D { Name = "VisualRoot" };
        PackedScene formalScene = GD.Load<PackedScene>("res://scenes/presentation/FallenBellkeeperFormalVisual.tscn");
        if (formalScene == null)
        {
            GD.PushError("BuildFallenBellkeeper: formal Boss visual must be built first.");
            Quit(1);
            return;
        }
        FallenBellkeeperFormalVisual formal = formalScene.Instantiate<FallenBellkeeperFormalVisual>();
        formal.Name = "Formal";
        formal.RemoveMeta("profile_bounds");
        visualRoot.AddChild(formal);
        visualRoot.AddChild(new AttackTelegraphVisual { Name = "AttackTelegraph", ZIndex = 6 });
        visualRoot.AddChild(new Node2D { Name = "Effects", ZIndex = 10 });
        boss.AddChild(visualRoot);
        boss.AddChild(new CollisionShape2D
        {
            Name = "BodyCollider",
            Shape = new CapsuleShape2D { Radius = 42f, Height = 122f }
        });

        const string path = "res://scenes/boss/FallenBellkeeper.tscn";
        Error error = SceneBuildUtil.PackAndSave(boss, path);
        boss.Free();
        if (error != Error.Ok) { GD.PushError($"BuildFallenBellkeeper failed: {error}"); Quit(1); return; }
        FallenBellkeeper copy = GD.Load<PackedScene>(path).Instantiate<FallenBellkeeper>();
        Node telegraphNode = copy.GetNodeOrNull("VisualRoot/AttackTelegraph");
        Node2D effects = copy.GetNodeOrNull<Node2D>("VisualRoot/Effects");
        bool valid = copy.GetNodeOrNull<HealthComponent>("Health")?.MaxHealth == 12 &&
            copy.GetNodeOrNull<ArmoredDamageReceiver>("Armor")?.Divisor == 3 &&
            copy.GetNodeOrNull<ThreadTarget2D>("ChainTarget") != null &&
            copy.GetNodeOrNull<ThreadTarget2D>("ChainTarget")?.Material == GrappleSurfaceMaterial.TempleMetal &&
            copy.GetNodeOrNull<LegacyThreadInteractionAdapter>(
                "ChainTarget/LegacyThreadInteractionAdapter") != null &&
            copy.GetNodeOrNull<FallenBellkeeperFormalVisual>("VisualRoot/Formal") is IEntityVisual2D &&
            telegraphNode is AttackTelegraphVisual && telegraphNode is not CollisionObject2D &&
            telegraphNode is not Control && ((Node2D)telegraphNode).ZIndex == 6 && effects?.ZIndex == 10 &&
            copy.FindChildren("*", "FallenBellkeeperPlaceholderVisual", true, false).Count == 0 &&
            copy.GetNode<Hitbox>("AttackHitbox").Damage == 1 &&
            copy.GetNode<Hitbox>("AttackHitbox").GuardClass == DamageGuardClass.Heavy && copy.Tuning.MaxHealth == 12 &&
            copy.Tuning.SweepTelegraphSeconds == .80f && copy.Tuning.SweepActiveSeconds == .25f &&
            copy.Tuning.SweepRecoverySeconds == .70f && copy.Tuning.LeapSlamTelegraphSeconds == .90f &&
            copy.Tuning.LeapSlamActiveSeconds == .25f && copy.Tuning.LeapSlamRecoverySeconds == .75f &&
            copy.Tuning.GroundWaveTelegraphSeconds == 1.00f && copy.Tuning.GroundWaveActiveSeconds == .30f &&
            copy.Tuning.GroundWaveRecoverySeconds == .75f && copy.Tuning.HammerThrowTelegraphSeconds == .85f &&
            copy.Tuning.HammerThrowActiveSeconds == .80f && copy.Tuning.HammerThrowRecoverySeconds == .70f &&
            copy.Tuning.ContactDamage == 1;
        copy.Free();
        if (!valid) { GD.PushError("BuildFallenBellkeeper contract validation failed."); Quit(1); return; }
        GD.Print("BuildFallenBellkeeper: packed and validated.");
        Quit(0);
    }
}
