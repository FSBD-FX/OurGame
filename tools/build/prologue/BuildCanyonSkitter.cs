using Godot;
using Knight.Abilities;
using Knight.Combat;
using Knight.Enemies.Skitter;
using Knight.Presentation;
using Knight.Tools;

namespace Knight.Build.Prologue;

public partial class BuildCanyonSkitter : SceneTree
{
    public override void _Initialize()
    {
        EnsureOwnedScriptUids();
        var settings = new CanyonSkitterSettings();
        var skitter = new CanyonSkitter
        {
            Name = "CanyonSkitter",
            CollisionLayer = CombatCollisionLayers.Enemy,
            CollisionMask = CombatCollisionLayers.World
        };
        skitter.AddChild(new CollisionShape2D
        {
            Name = "BodyCollider", Position = new Vector2(0f, -12f),
            Shape = new RectangleShape2D { Size = new Vector2(28f, 22f) }
        });
        skitter.AddChild(new HealthComponent
        {
            Name = "Health", MaxHealth = settings.MaxHealth, InvulnerabilitySeconds = 0f
        });
        var hurtbox = new Hurtbox
        {
            Name = "Hurtbox", CollisionLayer = CombatCollisionLayers.Enemy,
            CollisionMask = CombatCollisionLayers.PlayerAttack,
            Monitoring = true, Monitorable = true,
            DamageReceiverPath = new NodePath("../Health")
        };
        hurtbox.AddChild(new CollisionShape2D
        {
            Name = "HurtboxCollider", Position = new Vector2(0f, -12f),
            Shape = new RectangleShape2D { Size = new Vector2(28f, 22f) }
        });
        skitter.AddChild(hurtbox);
        var hitbox = new Hitbox
        {
            Name = "RushHitbox", CollisionLayer = CombatCollisionLayers.EnemyAttack,
            CollisionMask = CombatCollisionLayers.Player, Damage = settings.Damage,
            Knockback = 120f, GuardClass = DamageGuardClass.Normal,
            Monitoring = false, Monitorable = false
        };
        hitbox.AddChild(new CollisionShape2D
        {
            Name = "HitCollider", Shape = new RectangleShape2D { Size = new Vector2(16f, 16f) }
        });
        skitter.AddChild(hitbox);
        var target = new ThreadTarget2D
        {
            Name = "ThreadTarget", Kind = ThreadTargetKind.LightEnemy,
            PullReceiverPath = new NodePath(".."),
            Material = GrappleSurfaceMaterial.TempleMetal,
            AssistRadius = 26f
        };
        var targetArea = new Area2D
        {
            Name = "TargetCollision", CollisionLayer = CombatCollisionLayers.ThreadTarget,
            CollisionMask = 0, Monitoring = true, Monitorable = true
        };
        targetArea.AddChild(new CollisionShape2D
        {
            Name = "Collision", Position = new Vector2(0f, -12f),
            Shape = new CircleShape2D { Radius = 25f }
        });
        target.AddChild(targetArea);
        target.AddChild(new LegacyThreadInteractionAdapter { Name = "LegacyThreadInteractionAdapter" });
        skitter.AddChild(target);
        skitter.AddChild(new CanyonSkitterPresentation { Name = "Presentation" });
        var visualRoot = new Node2D { Name = "VisualRoot" };
        visualRoot.AddChild(new CanyonSkitterVisual
        {
            Name = "SkitterVisual", Scale = Vector2.One * 1.35f
        });
        visualRoot.AddChild(new AttackTelegraphVisual { Name = "AttackTelegraph", ZIndex = 6 });
        visualRoot.AddChild(new Node2D { Name = "Effects", ZIndex = 10 });
        skitter.AddChild(visualRoot);

        const string path = "res://scenes/enemies/CanyonSkitter.tscn";
        Error result = SceneBuildUtil.PackAndSave(skitter, path);
        skitter.Free();
        if (result != Error.Ok)
        {
            GD.PushError($"Canyon Skitter pack failed: {result}");
            Quit(1);
            return;
        }
        ResourceSaver.SetUid(path, ResourceUid.CreateIdForPath(path));
        CanyonSkitter copy = GD.Load<PackedScene>(path).Instantiate<CanyonSkitter>();
        bool valid = copy.CollisionLayer == CombatCollisionLayers.Enemy &&
            copy.CollisionMask == CombatCollisionLayers.World &&
            copy.GetNode<HealthComponent>("Health").MaxHealth == 2 &&
            copy.GetNode<ThreadTarget2D>("ThreadTarget").Kind == ThreadTargetKind.LightEnemy &&
            copy.GetNode<Hitbox>("RushHitbox").Damage == 1;
        copy.Free();
        if (!valid)
        {
            GD.PushError("Canyon Skitter scene round-trip contract failed.");
            Quit(1);
            return;
        }
        GD.Print("BUILD_CANYON_SKITTER_OK");
        Quit(0);
    }

    private static void EnsureOwnedScriptUids()
    {
        string[] paths =
        [
            "res://scripts/enemies/skitter/CanyonSkitterSettings.cs",
            "res://scripts/enemies/skitter/CanyonSkitterModel.cs",
            "res://scripts/enemies/skitter/CanyonSkitter.cs",
            "res://scripts/enemies/skitter/CanyonSkitterVisual.cs",
            "res://scripts/enemies/skitter/CanyonSkitterPresentation.cs",
            "res://tools/build/prologue/BuildCanyonSkitter.cs",
            "res://tests/unit/CanyonSkitterModelTests.cs",
            "res://tests/integration/CanyonSkitterRuntimeTests.cs",
            "res://tests/presentation/CanyonSkitterProbe.cs",
            "res://tests/RunCanyonSkitterTests.cs"
        ];
        foreach (string path in paths)
        {
            string sidecar = path + ".uid";
            if (FileAccess.FileExists(sidecar)) continue;
            long uid = ResourceUid.CreateIdForPath(path);
            if (!ResourceUid.HasId(uid)) ResourceUid.AddId(uid, path);
            using FileAccess file = FileAccess.Open(sidecar, FileAccess.ModeFlags.Write);
            if (file == null)
                throw new System.IO.IOException($"Cannot write required UID sidecar {sidecar}");
            file.StoreLine(ResourceUid.IdToText(uid));
        }
    }
}
