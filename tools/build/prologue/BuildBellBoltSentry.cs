using Godot;
using Knight.Abilities;
using Knight.Combat;
using Knight.Enemies.Sentry;
using Knight.Presentation;
using Knight.Tools;

namespace Knight.Build.Prologue;

public partial class BuildBellBoltSentry : SceneTree
{
    public override void _Initialize()
    {
        var settings = new BellBoltSentrySettings();
        var sentry = new BellBoltSentry
        {
            Name = "BellBoltSentry",
            CollisionLayer = CombatCollisionLayers.Enemy,
            CollisionMask = CombatCollisionLayers.World,
            Settings = settings
        };
        sentry.AddChild(new CollisionShape2D
        {
            Name = "BodyCollider",
            Shape = new CapsuleShape2D { Radius = 25f, Height = 68f },
            Position = new Vector2(0f, -5f)
        });
        sentry.AddChild(new HealthComponent
        {
            Name = "Health", MaxHealth = settings.MaxHealth, InvulnerabilitySeconds = 0f
        });
        var hurtbox = new Hurtbox
        {
            Name = "Hurtbox",
            CollisionLayer = CombatCollisionLayers.Enemy,
            CollisionMask = CombatCollisionLayers.PlayerAttack,
            Monitoring = true,
            Monitorable = true,
            DamageReceiverPath = new NodePath("../Health")
        };
        hurtbox.AddChild(new CollisionShape2D
        {
            Name = "HurtboxCollider",
            Shape = new CapsuleShape2D { Radius = 25f, Height = 68f },
            Position = new Vector2(0f, -5f)
        });
        sentry.AddChild(hurtbox);
        var target = new ThreadTarget2D
        {
            Name = "ThreadTarget",
            Kind = ThreadTargetKind.LightEnemy,
            PullReceiverPath = new NodePath(".."),
            Material = GrappleSurfaceMaterial.TempleMetal,
            AssistRadius = 34f
        };
        var targetArea = new Area2D
        {
            Name = "TargetCollision",
            CollisionLayer = CombatCollisionLayers.ThreadTarget,
            CollisionMask = 0,
            Monitoring = true,
            Monitorable = true
        };
        targetArea.AddChild(new CollisionShape2D
        {
            Name = "Collision", Shape = new CircleShape2D { Radius = 31f },
            Position = new Vector2(0f, -5f)
        });
        target.AddChild(targetArea);
        target.AddChild(new LegacyThreadInteractionAdapter { Name = "LegacyThreadInteractionAdapter" });
        sentry.AddChild(target);
        sentry.AddChild(new Marker2D { Name = "Muzzle", Position = new Vector2(0f, -8f) });
        sentry.AddChild(new BellBoltSentryPresentation { Name = "Presentation" });
        var visualRoot = new Node2D { Name = "VisualRoot", Position = new Vector2(0f, -5f) };
        visualRoot.AddChild(new BellBoltSentryVisual { Name = "SentryVisual" });
        visualRoot.AddChild(new AttackTelegraphVisual { Name = "AttackTelegraph", ZIndex = 6 });
        visualRoot.AddChild(new Node2D { Name = "Effects", ZIndex = 10 });
        sentry.AddChild(visualRoot);
        const string path = "res://scenes/enemies/BellBoltSentry.tscn";
        Error result = SceneBuildUtil.PackAndSave(sentry, path);
        sentry.Free();
        if (result != Error.Ok)
        {
            GD.PushError($"Sentry pack failed: {result}");
            Quit(1);
            return;
        }
        BellBoltSentry copy = GD.Load<PackedScene>(path).Instantiate<BellBoltSentry>();
        bool valid = copy.GetNode<HealthComponent>("Health").MaxHealth == 2 &&
            copy.GetNode<ThreadTarget2D>("ThreadTarget").Kind == ThreadTargetKind.LightEnemy &&
            copy.GetNodeOrNull<AttackTelegraphVisual>("VisualRoot/AttackTelegraph") != null &&
            copy.GetNodeOrNull<BellBoltSentryVisual>("VisualRoot/SentryVisual") != null;
        copy.Free();
        if (!valid)
        {
            GD.PushError("BuildBellBoltSentry contract validation failed.");
            Quit(1);
            return;
        }
        GD.Print("BuildBellBoltSentry: packed and round-trip validated actor scene.");
        Quit(0);
    }
}
