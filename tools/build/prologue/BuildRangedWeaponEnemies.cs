using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Godot;
using Knight.Abilities;
using Knight.Combat;
using Knight.Enemies.Ranged;
using Knight.Presentation;
using Knight.Tools;

namespace Knight.Build.Prologue;

public partial class BuildRangedWeaponEnemies : SceneTree
{
    public override void _Initialize()
    {
        try
        {
            string outputRoot = RequireOutputRoot();
            foreach (RangedWeaponRole role in Enum.GetValues<RangedWeaponRole>())
            {
                RangedWeaponEnemy actor = CreateActor(role);
                var sourceResources = CollectFactoryResources(actor);
                string path = Path.Combine(outputRoot, actor.GetType().Name + ".tscn");
                try
                {
                    AssertNoReparse(path);
                    if (File.Exists(path) || Directory.Exists(path))
                        throw new InvalidOperationException("Refusing existing staged scene: " + path);
                    int expectedNodes = CountNodes(actor);
                    Error error = SceneBuildUtil.PackAndSave(actor, path);
                    if (error != Error.Ok) throw new InvalidOperationException($"Ranged scene pack failed: {path}: {error}");
                    using PackedScene saved = ResourceLoader.Load<PackedScene>(path, cacheMode: ResourceLoader.CacheMode.Ignore);
                    if (saved == null) throw new InvalidOperationException("Ranged saved scene missing: " + path);
                    RangedWeaponEnemy copy = saved.Instantiate<RangedWeaponEnemy>();
                    var copyResources = CollectFactoryResources(copy);
                    try
                    {
                        if (CountNodes(copy) != expectedNodes || copy.Role != role || copy.Settings.Role != role ||
                            copy.GetNode<HealthComponent>("Health").MaxHealth != 2 ||
                            copy.GetNode<ThreadTarget2D>("ThreadTarget").Kind != ThreadTargetKind.LightEnemy ||
                            copy.GetNodeOrNull<RangedWeaponVisual>("VisualRoot/WeaponVisual") == null ||
                            copy.GetNodeOrNull<AttackTelegraphVisual>("VisualRoot/AttackTelegraph") == null)
                            throw new InvalidOperationException("Ranged scene round-trip contract failed: " + path);
                    }
                    finally { copy.Free(); foreach (Resource resource in copyResources) resource.Dispose(); }
                }
                finally { actor.Free(); foreach (Resource resource in sourceResources) resource.Dispose(); }
            }
            GD.Print("BuildRangedWeaponEnemies: four leaf scenes packed and node-count/role round-trip validated.");
            Quit(0);
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            Quit(1);
        }
    }

    private static string RequireOutputRoot()
    {
        const string argument = "--ranged-output-root=";
        string[] values = OS.GetCmdlineUserArgs().Where(value => value.StartsWith(argument, StringComparison.Ordinal)).ToArray();
        if (values.Length != 1) throw new InvalidOperationException("Require exactly one --ranged-output-root argument.");
        string supplied = values[0][argument.Length..];
        if (!Path.IsPathFullyQualified(supplied)) throw new InvalidOperationException("Ranged output root must be absolute.");
        string root = Path.GetFullPath(supplied).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        if (root.Equals(Path.GetPathRoot(root)?.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar), StringComparison.OrdinalIgnoreCase) ||
            !Path.IsPathFullyQualified(root))
            throw new InvalidOperationException("Ranged output root must be a case folder, not a filesystem root.");
        foreach (string protectedPath in new[] { ProjectSettings.GlobalizePath("res://"), OS.GetUserDataDir() })
        {
            string protectedRoot = Path.GetFullPath(protectedPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            if (Within(root, protectedRoot) || Within(protectedRoot, root))
                throw new InvalidOperationException("Ranged output root overlaps protected project/userdata ancestry.");
        }
        AssertNoReparse(root);
        if (!Directory.Exists(root) || Directory.EnumerateFileSystemEntries(root).Any())
            throw new InvalidOperationException("Ranged output root must be an existing empty case folder.");
        foreach (string name in new[] { nameof(RivetScatterGunner), nameof(BeltBurstGunner), nameof(FurnaceMissileGunner), nameof(PrismLaserGunner) })
        {
            string target = Path.GetFullPath(Path.Combine(root, name + ".tscn"));
            if (!Within(target, root)) throw new InvalidOperationException("Staged output escaped its admitted root.");
            AssertNoReparse(target);
            if (File.Exists(target) || Directory.Exists(target))
                throw new InvalidOperationException("Refusing existing staged scene: " + target);
        }
        return root;
    }

    private static bool Within(string path, string root) => path.Equals(root, StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);

    private static void AssertNoReparse(string path)
    {
        try
        {
            if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
                throw new InvalidOperationException("Ranged output cannot traverse a reparse leaf.");
        }
        catch (FileNotFoundException) { }
        catch (DirectoryNotFoundException) { }
        for (DirectoryInfo parent = new(path); parent != null; parent = parent.Parent)
            if (parent.Exists && (parent.Attributes & FileAttributes.ReparsePoint) != 0)
                throw new InvalidOperationException("Ranged output cannot traverse a reparse ancestor.");
    }

    // Callers own these newly-created shapes/settings. No loaded Player or
    // shared scene resources are created or disposed by this factory.
    public static RangedWeaponEnemy CreateActor(RangedWeaponRole role)
    {
        RangedWeaponEnemy actor = role switch
        {
            RangedWeaponRole.Scatter => new RivetScatterGunner(),
            RangedWeaponRole.Burst => new BeltBurstGunner(),
            RangedWeaponRole.Missile => new FurnaceMissileGunner(),
            RangedWeaponRole.Laser => new PrismLaserGunner(),
            _ => throw new ArgumentOutOfRangeException(nameof(role))
        };
        actor.Name = actor.GetType().Name;
        actor.Settings = RangedWeaponSettings.ForRole(role);
        actor.CollisionLayer = CombatCollisionLayers.Enemy;
        actor.CollisionMask = CombatCollisionLayers.World;
        actor.AddChild(new CollisionShape2D
        {
            Name = "BodyCollider", Position = new Vector2(0f, -5f),
            Shape = new CapsuleShape2D { Radius = 25f, Height = 68f }
        });
        actor.AddChild(new HealthComponent
        {
            Name = "Health", MaxHealth = 2, InvulnerabilitySeconds = 0f
        });
        var hurtbox = new Hurtbox
        {
            Name = "Hurtbox", CollisionLayer = CombatCollisionLayers.Enemy,
            CollisionMask = CombatCollisionLayers.PlayerAttack,
            Monitoring = true, Monitorable = true, DamageReceiverPath = new NodePath("../Health")
        };
        hurtbox.AddChild(new CollisionShape2D
        {
            Name = "HurtboxCollider", Position = new Vector2(0f, -5f),
            Shape = new CapsuleShape2D { Radius = 25f, Height = 68f }
        });
        actor.AddChild(hurtbox);
        var target = new ThreadTarget2D
        {
            Name = "ThreadTarget", Kind = ThreadTargetKind.LightEnemy,
            PullReceiverPath = new NodePath(".."), Material = GrappleSurfaceMaterial.TempleMetal,
            AssistRadius = 34f
        };
        var targetArea = new Area2D
        {
            Name = "TargetCollision", CollisionLayer = CombatCollisionLayers.ThreadTarget,
            CollisionMask = 0, Monitoring = true, Monitorable = true
        };
        targetArea.AddChild(new CollisionShape2D
        {
            Name = "Collision", Position = new Vector2(0f, -5f),
            Shape = new CircleShape2D { Radius = 31f }
        });
        target.AddChild(targetArea);
        target.AddChild(new LegacyThreadInteractionAdapter { Name = "LegacyThreadInteractionAdapter" });
        actor.AddChild(target);
        actor.AddChild(new Marker2D { Name = "Muzzle", Position = new Vector2(0f, -8f) });
        actor.AddChild(new RangedWeaponPresentation { Name = "Presentation" });
        var visual = new Node2D { Name = "VisualRoot", Position = new Vector2(0f, -5f) };
        visual.AddChild(new RangedWeaponVisual { Name = "WeaponVisual", Role = role });
        visual.AddChild(new AttackTelegraphVisual { Name = "AttackTelegraph", ZIndex = 6 });
        visual.AddChild(new Node2D { Name = "Effects", ZIndex = 10 });
        actor.AddChild(visual);
        return actor;
    }

    private static HashSet<Resource> CollectFactoryResources(RangedWeaponEnemy actor)
    {
        var result = new HashSet<Resource> { actor.Settings };
        CollectShapes(actor, result);
        return result;
    }

    private static void CollectShapes(Node node, HashSet<Resource> result)
    {
        if (node is CollisionShape2D collider && collider.Shape != null) result.Add(collider.Shape);
        foreach (Node child in node.GetChildren()) CollectShapes(child, result);
    }

    private static int CountNodes(Node node)
    {
        int count = 1;
        foreach (Node child in node.GetChildren()) count += CountNodes(child);
        return count;
    }
}
