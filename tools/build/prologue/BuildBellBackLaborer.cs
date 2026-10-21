using Godot;
using Knight.Abilities;
using Knight.Combat;
using Knight.Config;
using Knight.Enemies.Laborer;
using Knight.Presentation;
using Knight.Tools;

namespace Knight.Build.Prologue;

public partial class BuildBellBackLaborer : SceneTree
{
    public override void _Initialize()
    {
        var tuning = new BellBackLaborerTuning
        {
            TelegraphSeconds = .75f,
            AttackSeconds = .20f,
            RecoverySeconds = .70f,
            ContactDamage = 1
        };
        var laborer = new BellBackLaborer
        {
            Name = "BellBackLaborer",
            CollisionLayer = CombatCollisionLayers.Enemy,
            CollisionMask = CombatCollisionLayers.World,
            Tuning = tuning
        };
        var health = new HealthComponent { Name = "Health", MaxHealth = 6, InvulnerabilitySeconds = 0f };
        var armor = new ArmoredDamageReceiver { Name = "Armor", HealthPath = new NodePath("../Health"), Divisor = 3 };
        var hurtbox = new Hurtbox
        {
            Name = "Hurtbox", CollisionLayer = CombatCollisionLayers.Enemy,
            CollisionMask = 0, Monitoring = true, Monitorable = true,
            DamageReceiverPath = new NodePath("../Armor")
        };
        hurtbox.AddChild(new CollisionShape2D
        {
            Name = "HurtboxCollider", Shape = new CapsuleShape2D { Radius = 28f, Height = 84f }
        });
        var attack = new Hitbox
        {
            Name = "AttackHitbox", CollisionLayer = CombatCollisionLayers.EnemyAttack,
            CollisionMask = CombatCollisionLayers.Player, Damage = 1, Knockback = 240f,
            Monitoring = false, Monitorable = false
        };
        attack.AddChild(new CollisionShape2D
        {
            Name = "AttackCollider", Shape = BellBackLaborerSweepGeometry.CreateShape()
        });
        var target = new ThreadTarget2D
        {
            Name = "ThreadTarget", Kind = ThreadTargetKind.HeavyEnemy,
            PullReceiverPath = new NodePath(".."), Material = GrappleSurfaceMaterial.TempleMetal,
            AssistRadius = MechanicalClawSettings.Default.MainAnchorAssistRadius
        };
        var targetArea = new Area2D
        {
            Name = "TargetCollision", CollisionLayer = CombatCollisionLayers.ThreadTarget,
            CollisionMask = 0, Monitoring = true
        };
        targetArea.AddChild(new CollisionShape2D
        {
            Name = "Collision", Shape = new CircleShape2D { Radius = 32f }
        });
        target.AddChild(targetArea);
        target.AddChild(new LegacyThreadInteractionAdapter { Name = "LegacyThreadInteractionAdapter" });
        var presentation = new BellBackLaborerPresentationCoordinator
        {
            Name = "Presentation", Profile = new PresentationProfile()
        };
        var visualRoot = new Node2D { Name = "VisualRoot" };
        PackedScene formalScene = GD.Load<PackedScene>("res://scenes/presentation/BellBackLaborerFormalVisual.tscn");
        if (formalScene == null)
        {
            GD.PushError("BuildBellBackLaborer: formal enemy visuals must be built first.");
            Quit(1);
            return;
        }
        BellBackLaborerFormalVisual visual = formalScene.Instantiate<BellBackLaborerFormalVisual>();
        visual.Name = "Formal";
        visualRoot.AddChild(visual);
        visualRoot.AddChild(new AttackTelegraphVisual { Name = "AttackTelegraph", ZIndex = 6 });
        visualRoot.AddChild(new Node2D { Name = "Effects", ZIndex = 10 });
        laborer.AddChild(health); laborer.AddChild(armor); laborer.AddChild(hurtbox); laborer.AddChild(attack);
        laborer.AddChild(target); laborer.AddChild(presentation); laborer.AddChild(visualRoot);
        laborer.AddChild(new CollisionShape2D
        {
            Name = "BodyCollider", Shape = new CapsuleShape2D { Radius = 28f, Height = 84f }
        });

        const string path = "res://scenes/enemies/BellBackLaborer.tscn";
        Error error = SceneBuildUtil.PackAndSave(laborer, path); laborer.Free();
        if (error != Error.Ok) { GD.PushError($"BuildBellBackLaborer failed: {error}"); Quit(1); return; }
        BellBackLaborer copy = GD.Load<PackedScene>(path).Instantiate<BellBackLaborer>();
        Node telegraphNode = copy.GetNodeOrNull("VisualRoot/AttackTelegraph");
        Node2D effects = copy.GetNodeOrNull<Node2D>("VisualRoot/Effects");
        bool valid = copy.GetNodeOrNull<HealthComponent>("Health")?.MaxHealth == 6 &&
            copy.GetNodeOrNull<ArmoredDamageReceiver>("Armor")?.Divisor == 3 &&
            copy.GetNodeOrNull<Hurtbox>("Hurtbox")?.DamageReceiverPath == new NodePath("../Armor") &&
            copy.GetNodeOrNull<ThreadTarget2D>("ThreadTarget")?.Kind == ThreadTargetKind.HeavyEnemy &&
            copy.GetNodeOrNull<ThreadTarget2D>("ThreadTarget")?.Material == GrappleSurfaceMaterial.TempleMetal &&
            copy.GetNodeOrNull<LegacyThreadInteractionAdapter>(
                "ThreadTarget/LegacyThreadInteractionAdapter") != null &&
            copy.GetNodeOrNull<BellBackLaborerFormalVisual>("VisualRoot/Formal") is IEntityVisual2D &&
            telegraphNode is AttackTelegraphVisual && telegraphNode is not CollisionObject2D &&
            telegraphNode is not Control && ((Node2D)telegraphNode).ZIndex == 6 && effects?.ZIndex == 10 &&
            copy.FindChildren("*", "BellBackLaborerPlaceholderVisual", true, false).Count == 0 &&
            copy.GetNode<Hitbox>("AttackHitbox").Damage == 1 && copy.Tuning.TelegraphSeconds == .75f &&
            copy.Tuning.AttackSeconds == .20f && copy.Tuning.RecoverySeconds == .70f &&
            copy.Tuning.ContactDamage == 1 &&
            copy.GetNode<CollisionShape2D>("AttackHitbox/AttackCollider").Shape is ConvexPolygonShape2D fan &&
            fan.Points.Length == BellBackLaborerSweepGeometry.ArcSegments + 2;
        copy.Free();
        if (!valid) { GD.PushError("BuildBellBackLaborer contract validation failed."); Quit(1); return; }
        GD.Print("BuildBellBackLaborer: packed and validated."); Quit(0);
    }
}
