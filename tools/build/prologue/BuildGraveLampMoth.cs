using Godot;
using Knight.Abilities;
using Knight.Combat;
using Knight.Config;
using Knight.Enemies.Moth;
using Knight.Presentation;
using Knight.Tools;

namespace Knight.Build.Prologue;

public partial class BuildGraveLampMoth : SceneTree
{
    public override void _Initialize()
    {
        var tuning = new EnemyArchetypeTuning
        {
            MaxHealth = 2,
            TelegraphSeconds = .70f,
            AttackSeconds = .35f,
            RecoverySeconds = .70f,
            ContactDamage = 1
        };
        var moth = new GraveLampMoth
        {
            Name = "GraveLampMoth",
            CollisionLayer = CombatCollisionLayers.Enemy,
            CollisionMask = CombatCollisionLayers.World,
            Tuning = tuning
        };
        var health = new HealthComponent { Name = "Health", MaxHealth = tuning.MaxHealth };
        var hurtbox = new Hurtbox
        {
            Name = "Hurtbox", CollisionLayer = CombatCollisionLayers.Enemy,
            CollisionMask = CombatCollisionLayers.None, Monitoring = true, Monitorable = true
        };
        hurtbox.AddChild(new CollisionShape2D
        {
            Name = "HurtboxCollider", Shape = new CircleShape2D { Radius = 18f }
        });
        var attack = new Hitbox
        {
            Name = "AttackHitbox", CollisionLayer = CombatCollisionLayers.EnemyAttack,
            CollisionMask = CombatCollisionLayers.Player, Damage = 1, Knockback = 180f,
            Monitoring = false, Monitorable = false
        };
        attack.AddChild(new CollisionShape2D
        {
            Name = "AttackCollider", Shape = new CircleShape2D { Radius = 22f }
        });
        var target = new ThreadTarget2D
        {
            Name = "ThreadTarget", Kind = ThreadTargetKind.LightEnemy,
            PullReceiverPath = new NodePath(".."), Material = GrappleSurfaceMaterial.TempleMetal,
            AssistRadius = MechanicalClawSettings.Default.MainAnchorAssistRadius
        };
        var targetArea = new Area2D
        {
            Name = "TargetCollision", CollisionLayer = CombatCollisionLayers.ThreadTarget,
            CollisionMask = CombatCollisionLayers.None, Monitoring = true, Monitorable = true
        };
        targetArea.AddChild(new CollisionShape2D
        {
            Name = "Collision", Shape = new CircleShape2D { Radius = 24f }
        });
        target.AddChild(targetArea);
        target.AddChild(new LegacyThreadInteractionAdapter { Name = "LegacyThreadInteractionAdapter" });
        var presentation = new GraveLampMothPresentationCoordinator
        {
            Name = "Presentation", Profile = new PresentationProfile()
        };
        var visualRoot = new Node2D { Name = "VisualRoot" };
        PackedScene formalScene = GD.Load<PackedScene>("res://scenes/presentation/GraveLampMothFormalVisual.tscn");
        if (formalScene == null)
        {
            GD.PushError("BuildGraveLampMoth: formal enemy visuals must be built first.");
            Quit(1);
            return;
        }
        GraveLampMothFormalVisual visual = formalScene.Instantiate<GraveLampMothFormalVisual>();
        visual.Name = "Formal";
        visualRoot.AddChild(visual);
        visualRoot.AddChild(new AttackTelegraphVisual { Name = "AttackTelegraph", ZIndex = 6 });
        visualRoot.AddChild(new Node2D { Name = "Effects", ZIndex = 10 });
        moth.AddChild(health);
        moth.AddChild(hurtbox);
        moth.AddChild(attack);
        moth.AddChild(target);
        moth.AddChild(presentation);
        moth.AddChild(visualRoot);
        moth.AddChild(new CollisionShape2D
        {
            Name = "BodyCollider", Shape = new CircleShape2D { Radius = 16f }
        });

        Error error = SceneBuildUtil.PackAndSave(moth, "res://scenes/enemies/GraveLampMoth.tscn");
        moth.Free();
        if (error != Error.Ok) { GD.PushError($"BuildGraveLampMoth failed: {error}"); Quit(1); return; }
        GraveLampMoth copy = GD.Load<PackedScene>("res://scenes/enemies/GraveLampMoth.tscn")
            .Instantiate<GraveLampMoth>();
        Node telegraphNode = copy.GetNodeOrNull("VisualRoot/AttackTelegraph");
        Node2D effects = copy.GetNodeOrNull<Node2D>("VisualRoot/Effects");
        bool valid = copy.GetNodeOrNull<HealthComponent>("Health")?.MaxHealth == 2 &&
            copy.GetNodeOrNull<Hurtbox>("Hurtbox")?.CollisionLayer == CombatCollisionLayers.Enemy &&
            copy.GetNodeOrNull<Hitbox>("AttackHitbox")?.CollisionMask == CombatCollisionLayers.Player &&
            copy.GetNodeOrNull<ThreadTarget2D>("ThreadTarget")?.Kind == ThreadTargetKind.LightEnemy &&
            copy.GetNodeOrNull<ThreadTarget2D>("ThreadTarget")?.Material == GrappleSurfaceMaterial.TempleMetal &&
            copy.GetNodeOrNull<LegacyThreadInteractionAdapter>(
                "ThreadTarget/LegacyThreadInteractionAdapter") != null &&
            copy.GetNodeOrNull<Area2D>("ThreadTarget/TargetCollision")?.CollisionLayer ==
                CombatCollisionLayers.ThreadTarget &&
            copy.GetNodeOrNull<GraveLampMothPresentationCoordinator>("Presentation")?.Profile != null &&
            copy.GetNodeOrNull<GraveLampMothFormalVisual>("VisualRoot/Formal") is IEntityVisual2D &&
            telegraphNode is AttackTelegraphVisual && telegraphNode is not CollisionObject2D &&
            telegraphNode is not Control && ((Node2D)telegraphNode).ZIndex == 6 && effects?.ZIndex == 10 &&
            copy.FindChildren("*", "GraveLampMothPlaceholderVisual", true, false).Count == 0 &&
            effects != null && copy.GetNode<Hitbox>("AttackHitbox").Damage == 1 &&
            copy.Tuning.MaxHealth == 2 && copy.Tuning.TelegraphSeconds == .70f &&
            copy.Tuning.AttackSeconds == .35f && copy.Tuning.RecoverySeconds == .70f &&
            copy.Tuning.ContactDamage == 1;
        copy.Free();
        if (!valid) { GD.PushError("BuildGraveLampMoth contract validation failed."); Quit(1); return; }
        GD.Print("BuildGraveLampMoth: packed and validated.");
        Quit(0);
    }
}
