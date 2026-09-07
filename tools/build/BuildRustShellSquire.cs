using Godot;
using Knight.Abilities;
using Knight.Combat;
using Knight.Config;
using Knight.Enemies;
using Knight.Presentation;
using Knight.Tools;

namespace Knight.Build;

/// <summary>Build-only generator for the independently instantiable Rust-Shell Squire scene.</summary>
public partial class BuildRustShellSquire : SceneTree
{
    public override void _Initialize()
    {
        var presentationProfile = new PresentationProfile();
        var squire = new RustShellSquire
        {
            Name = "RustShellSquire",
            CollisionLayer = CombatCollisionLayers.Enemy,
            CollisionMask = CombatCollisionLayers.World
        };
        var health = new HealthComponent { Name = "Health", MaxHealth = 3 };
        var hurtbox = new Hurtbox
        {
            Name = "Hurtbox",
            Monitoring = true,
            Monitorable = true,
            CollisionLayer = CombatCollisionLayers.Enemy,
            CollisionMask = CombatCollisionLayers.None
        };
        var hurtboxCollider = new CollisionShape2D
        {
            Name = "HurtboxCollider",
            Shape = new CapsuleShape2D { Radius = 18f, Height = 64f }
        };
        var attackHitbox = new Hitbox
        {
            Name = "AttackHitbox",
            Monitoring = false,
            Monitorable = false,
            CollisionLayer = CombatCollisionLayers.EnemyAttack,
            CollisionMask = CombatCollisionLayers.Player,
            Damage = 1,
            Knockback = 190f
        };
        var attackCollider = new CollisionShape2D
        {
            Name = "AttackCollider",
            Shape = new RectangleShape2D { Size = new Vector2(26f, 18f) }
        };
        var threadTarget = new ThreadTarget2D
        {
            Name = "ThreadTarget",
            Kind = ThreadTargetKind.LightEnemy,
            Material = GrappleSurfaceMaterial.TempleMetal,
            AssistRadius = 36f,
            PullReceiverPath = new NodePath("..")
        };
        var threadTargetCollision = new Area2D
        {
            Name = "TargetCollision",
            CollisionLayer = CombatCollisionLayers.ThreadTarget,
            CollisionMask = CombatCollisionLayers.None,
            Monitoring = true,
            Monitorable = true
        };
        var threadTargetShape = new CollisionShape2D
        {
            Name = "Collision",
            Shape = new CircleShape2D { Radius = 18f }
        };
        var legacyThreadAdapter = new LegacyThreadInteractionAdapter
        {
            Name = "LegacyThreadInteractionAdapter",
            TargetPath = new NodePath("..")
        };
        var presentation = new RustShellPresentationCoordinator { Name = "Presentation", Profile = presentationProfile };
        var visualRoot = new Node2D { Name = "VisualRoot" };
        PackedScene formalScene = GD.Load<PackedScene>("res://scenes/presentation/RustShellFormalVisual.tscn");
        if (formalScene == null)
        {
            GD.PushError("BuildRustShellSquire: formal enemy visuals must be built first.");
            Quit(1);
            return;
        }
        RustShellFormalVisual visual = formalScene.Instantiate<RustShellFormalVisual>();
        visual.Name = "Formal";
        var attackTelegraph = new AttackTelegraphVisual { Name = "AttackTelegraph", ZIndex = 6 };
        var effects = new Node2D { Name = "Effects", ZIndex = 10 };
        var bodyCollider = new CollisionShape2D
        {
            Name = "BodyCollider",
            Shape = new CapsuleShape2D { Radius = 18f, Height = 64f }
        };

        squire.AddChild(health);
        squire.AddChild(hurtbox);
        hurtbox.AddChild(hurtboxCollider);
        squire.AddChild(attackHitbox);
        attackHitbox.AddChild(attackCollider);
        squire.AddChild(threadTarget);
        threadTarget.AddChild(threadTargetCollision);
        threadTargetCollision.AddChild(threadTargetShape);
        threadTarget.AddChild(legacyThreadAdapter);
        squire.AddChild(presentation);
        squire.AddChild(visualRoot);
        visualRoot.AddChild(visual);
        visualRoot.AddChild(attackTelegraph);
        visualRoot.AddChild(effects);
        squire.AddChild(bodyCollider);

        int prePackCount = CountNodes(squire);
        Error saveResult = SceneBuildUtil.PackAndSave(squire, "res://scenes/enemies/RustShellSquire.tscn");
        squire.Free();
        if (saveResult != Error.Ok)
        {
            GD.PushError($"BuildRustShellSquire: failed to save RustShellSquire.tscn: {saveResult}");
            Quit(1);
            return;
        }

        PackedScene packed = ResourceLoader.Load<PackedScene>("res://scenes/enemies/RustShellSquire.tscn");
        Node copy = packed.Instantiate();
        int postInstantiateCount = CountNodes(copy);
        Hurtbox generatedHurtbox = copy.GetNodeOrNull<Hurtbox>("Hurtbox");
        Hitbox generatedHitbox = copy.GetNodeOrNull<Hitbox>("AttackHitbox");
        HealthComponent generatedHealth = copy.GetNodeOrNull<HealthComponent>("Health");
        ThreadTarget2D generatedThreadTarget = copy.GetNodeOrNull<ThreadTarget2D>("ThreadTarget");
        Area2D generatedTargetCollision = copy.GetNodeOrNull<Area2D>("ThreadTarget/TargetCollision");
        CollisionShape2D generatedTargetShape = copy.GetNodeOrNull<CollisionShape2D>(
            "ThreadTarget/TargetCollision/Collision");
        LegacyThreadInteractionAdapter generatedAdapter = copy.GetNodeOrNull<LegacyThreadInteractionAdapter>(
            "ThreadTarget/LegacyThreadInteractionAdapter");
        Node generatedTelegraphNode = copy.GetNodeOrNull("VisualRoot/AttackTelegraph");
        Node2D generatedEffects = copy.GetNodeOrNull<Node2D>("VisualRoot/Effects");
        RustShellSettings tuning = new();
        bool hasRequiredPaths = copy.GetNodeOrNull<HealthComponent>("Health") != null &&
            generatedHurtbox != null && copy.GetNodeOrNull<Node>("Hurtbox/HurtboxCollider") != null &&
            generatedHitbox != null && copy.GetNodeOrNull<Node>("AttackHitbox/AttackCollider") != null &&
            generatedThreadTarget != null &&
            generatedTargetCollision != null && generatedTargetShape?.Shape != null && generatedAdapter != null &&
            copy.GetNodeOrNull<RustShellPresentationCoordinator>("Presentation") != null &&
            copy.GetNodeOrNull<RustShellFormalVisual>("VisualRoot/Formal") is IEntityVisual2D &&
            generatedTelegraphNode is AttackTelegraphVisual &&
            copy.FindChildren("*", "RustShellPlaceholderVisual", true, false).Count == 0 &&
            generatedEffects != null &&
            copy.GetNodeOrNull<Node>("BodyCollider") != null;
        bool hasExpectedFilters = generatedHurtbox != null && generatedHitbox != null &&
            generatedHurtbox.CollisionLayer == CombatCollisionLayers.Enemy &&
            generatedHurtbox.CollisionMask == CombatCollisionLayers.None &&
            generatedHitbox.CollisionLayer == CombatCollisionLayers.EnemyAttack &&
            generatedHitbox.CollisionMask == CombatCollisionLayers.Player;
        bool hasThreadContract = generatedThreadTarget != null && generatedThreadTarget.Kind == ThreadTargetKind.LightEnemy &&
            generatedThreadTarget.Material == GrappleSurfaceMaterial.TempleMetal &&
            Mathf.IsEqualApprox(generatedThreadTarget.AssistRadius, 36f) &&
            generatedThreadTarget.PullReceiverPath == new NodePath("..") &&
            generatedTargetCollision?.CollisionLayer == CombatCollisionLayers.ThreadTarget &&
            generatedTargetCollision.CollisionMask == CombatCollisionLayers.None &&
            generatedAdapter?.TargetPath == new NodePath("..");
        bool hasPresentationProfile = copy.GetNode<RustShellPresentationCoordinator>("Presentation").Profile != null;
        bool hasNeutralPresentationLayer = generatedTelegraphNode is not CollisionObject2D &&
            generatedTelegraphNode is not Control && generatedTelegraphNode is Node2D generatedTelegraph &&
            generatedTelegraph.ZIndex == 6 && generatedEffects?.ZIndex == 10;
        bool hasApprovedTuning = generatedHealth?.MaxHealth == 3 && generatedHitbox?.Damage == 1 &&
            tuning.TelegraphSeconds == .65f && tuning.AttackSeconds == .14f &&
            tuning.RecoverySeconds == .65f && tuning.ContactDamage == 1;
        copy.Free();
        if (prePackCount != postInstantiateCount || !hasRequiredPaths || !hasExpectedFilters || !hasThreadContract ||
            !hasPresentationProfile || !hasNeutralPresentationLayer || !hasApprovedTuning)
        {
            GD.PushError("BuildRustShellSquire: scene validation failed.");
            Quit(1);
            return;
        }

        GD.Print($"BuildRustShellSquire: pre-pack node count={prePackCount}; post-instantiate node count={postInstantiateCount}");
        Quit(0);
    }

    private static int CountNodes(Node node)
    {
        int count = 1;
        foreach (Node child in node.GetChildren()) count += CountNodes(child);
        return count;
    }
}
