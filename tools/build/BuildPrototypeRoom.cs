using Godot;
using Knight.Abilities;
using Knight.Combat;
using Knight.Enemies;
using Knight.Tools;
using Knight.World;

namespace Knight.Build;

/// <summary>Build-only scene generator for the authored gray-box belfry traversal route.</summary>
public partial class BuildPrototypeRoom : SceneTree
{
    private const uint WorldLayer = CombatCollisionLayers.World;
    private const uint PlayerLayer = CombatCollisionLayers.Player;
    private const uint EnemyLayer = CombatCollisionLayers.Enemy;
    private const uint PlayerAttackLayer = CombatCollisionLayers.PlayerAttack;
    private const uint EnemyAttackLayer = CombatCollisionLayers.EnemyAttack;
    private const uint ThreadTargetLayer = CombatCollisionLayers.ThreadTarget;

    public override void _Initialize()
    {
        var room = new PrototypeRoomController { Name = "PrototypeRoom" };
        room.AddChild(Backdrop());
        var geometry = new Node2D { Name = "Geometry" };
        room.AddChild(geometry);
        Platform(geometry, "SpawnPlatform", new Vector2(-405f, 250f), new Vector2(310f, 30f));
        Platform(geometry, "StepOne", new Vector2(-120f, 170f), new Vector2(120f, 30f));
        Platform(geometry, "StepTwo", new Vector2(80f, 80f), new Vector2(120f, 30f));
        Platform(geometry, "LandingPlatform", new Vector2(520f, 80f), new Vector2(320f, 30f));
        Platform(geometry, "LowCeiling", new Vector2(440f, -40f), new Vector2(180f, 24f));

        CharacterBody2D player = InstancePlayer();
        player.Name = "Player";
        player.GlobalPosition = new Vector2(-480f, 198f);
        room.AddChild(player);

        var actors = new Node2D { Name = "Actors" };
        room.AddChild(actors);
        RustShellSquire enemy = InstanceEnemy();
        enemy.Name = "RustShellSquire";
        enemy.GlobalPosition = new Vector2(520f, 28f);
        enemy.AddToGroup("resettable_enemies", true);
        actors.AddChild(enemy);

        geometry.AddChild(ThreadAnchor());
        room.AddChild(BellTarget());
        room.AddChild(Checkpoint());
        room.AddChild(KillZone());
        room.AddChild(ExitMarker());
        room.AddChild(BuildHud());

        Error save = SceneBuildUtil.PackAndSave(room, "res://scenes/prototype/PrototypeRoom.tscn");
        room.Free();
        if (save != Error.Ok)
        {
            GD.PushError($"BuildPrototypeRoom: failed to validate/save scene: {save}");
            Quit(1);
            return;
        }

        PackedScene generated = ResourceLoader.Load<PackedScene>("res://scenes/prototype/PrototypeRoom.tscn");
        Node copy = generated.Instantiate();
        bool contractsPresent = ValidateGeneratedRoom(copy);
        copy.Free();
        if (!contractsPresent)
        {
            GD.PushError("BuildPrototypeRoom: generated scene is missing a route contract.");
            Quit(1);
            return;
        }

        ProjectSettings.SetSetting("application/run/main_scene", "res://scenes/prototype/PrototypeRoom.tscn");
        ProjectSettings.SetSetting("physics/common/physics_ticks_per_second", 60);
        ProjectSettings.SetInitialValue("physics/common/physics_ticks_per_second", 0);
        Error settingsSave = ProjectSettings.Save();
        if (settingsSave != Error.Ok)
        {
            GD.PushError($"BuildPrototypeRoom: unable to set main scene: {settingsSave}");
            Quit(1);
            return;
        }

        GD.Print("BuildPrototypeRoom: gray-box route packed, instantiated, and set as main scene.");
        Quit(0);
    }

    private static bool ValidateGeneratedRoom(Node room)
    {
        CharacterBody2D player = room.GetNodeOrNull<CharacterBody2D>("Player");
        RustShellSquire enemy = room.GetNodeOrNull<RustShellSquire>("Actors/RustShellSquire");
        ThreadTarget2D anchor = room.GetNodeOrNull<ThreadTarget2D>("Geometry/ThreadAnchor");
        StaticBody2D spawn = room.GetNodeOrNull<StaticBody2D>("Geometry/SpawnPlatform");
        StaticBody2D stepOne = room.GetNodeOrNull<StaticBody2D>("Geometry/StepOne");
        StaticBody2D stepTwo = room.GetNodeOrNull<StaticBody2D>("Geometry/StepTwo");
        StaticBody2D landing = room.GetNodeOrNull<StaticBody2D>("Geometry/LandingPlatform");
        if (player == null || enemy == null || anchor == null || spawn == null || stepOne == null ||
            stepTwo == null || landing == null) return false;

        return player.CollisionLayer == PlayerLayer && player.CollisionMask == WorldLayer &&
            enemy.CollisionLayer == EnemyLayer && enemy.CollisionMask == WorldLayer &&
            player.GetNode<Hurtbox>("Hurtbox").CollisionLayer == PlayerLayer &&
            enemy.GetNode<Hurtbox>("Hurtbox").CollisionLayer == EnemyLayer &&
            player.GetNode<Hitbox>("Combat/AttackHitbox").CollisionLayer == PlayerAttackLayer &&
            player.GetNode<Hitbox>("Combat/AttackHitbox").CollisionMask == EnemyLayer &&
            enemy.GetNode<Hitbox>("AttackHitbox").CollisionLayer == EnemyAttackLayer &&
            enemy.GetNode<Hitbox>("AttackHitbox").CollisionMask == PlayerLayer &&
            PlatformMatches(spawn, new Vector2(-405f, 250f), new Vector2(310f, 30f)) &&
            PlatformMatches(stepOne, new Vector2(-120f, 170f), new Vector2(120f, 30f)) &&
            PlatformMatches(stepTwo, new Vector2(80f, 80f), new Vector2(120f, 30f)) &&
            PlatformMatches(landing, new Vector2(520f, 80f), new Vector2(320f, 30f)) &&
            anchor.Position == new Vector2(260f, -80f) &&
            room.GetNodeOrNull<Hurtbox>("BellTarget/Hurtbox") != null &&
            room.GetNodeOrNull<Label>("Hud/Status") != null &&
            room.GetNodeOrNull<Area2D>("Checkpoint")?.CollisionMask == PlayerLayer &&
            room.GetNodeOrNull<KillZone>("KillZone")?.CollisionMask == PlayerLayer &&
            room.GetNodeOrNull<Area2D>("ExitMarker")?.CollisionMask == PlayerLayer;
    }

    private static bool PlatformMatches(StaticBody2D platform, Vector2 position, Vector2 size)
    {
        RectangleShape2D shape = platform.GetNodeOrNull<CollisionShape2D>("Collision")?.Shape as RectangleShape2D;
        return platform.Position == position && platform.CollisionLayer == WorldLayer &&
            platform.CollisionMask == 0 && shape != null && shape.Size == size;
    }

    private static CharacterBody2D InstancePlayer()
    {
        PackedScene scene = GD.Load<PackedScene>("res://scenes/player/Player.tscn");
        CharacterBody2D player = scene.Instantiate<CharacterBody2D>();
        player.CollisionLayer = PlayerLayer;
        player.CollisionMask = WorldLayer;
        player.AddToGroup("player", true);
        Hitbox attack = player.GetNode<Hitbox>("Combat/AttackHitbox");
        Hurtbox hurtbox = player.GetNode<Hurtbox>("Hurtbox");
        hurtbox.CollisionLayer = PlayerLayer;
        hurtbox.CollisionMask = 0;
        attack.CollisionLayer = PlayerAttackLayer;
        attack.CollisionMask = EnemyLayer;
        return player;
    }

    private static RustShellSquire InstanceEnemy()
    {
        PackedScene scene = GD.Load<PackedScene>("res://scenes/enemies/RustShellSquire.tscn");
        RustShellSquire enemy = scene.Instantiate<RustShellSquire>();
        enemy.CollisionLayer = EnemyLayer;
        enemy.CollisionMask = WorldLayer;
        Hitbox attack = enemy.GetNode<Hitbox>("AttackHitbox");
        Hurtbox hurtbox = enemy.GetNode<Hurtbox>("Hurtbox");
        hurtbox.CollisionLayer = EnemyLayer;
        hurtbox.CollisionMask = 0;
        attack.CollisionLayer = EnemyAttackLayer;
        attack.CollisionMask = PlayerLayer;
        return enemy;
    }

    private static StaticBody2D Platform(Node parent, string name, Vector2 center, Vector2 size)
    {
        var body = new StaticBody2D { Name = name, Position = center, CollisionLayer = WorldLayer, CollisionMask = 0 };
        var collision = new CollisionShape2D { Name = "Collision", Shape = new RectangleShape2D { Size = size } };
        var visual = new Polygon2D
        {
            Name = "Visual",
            Color = new Color("29463c"),
            Polygon = [-size / 2f, new Vector2(size.X / 2f, -size.Y / 2f), size / 2f, new Vector2(-size.X / 2f, size.Y / 2f)]
        };
        var rim = new Polygon2D
        {
            Name = "ReadabilityRim",
            Color = new Color("9fc48d"),
            Polygon = [new Vector2(-size.X / 2f, -size.Y / 2f), new Vector2(size.X / 2f, -size.Y / 2f),
                new Vector2(size.X / 2f, -size.Y / 2f + 4f), new Vector2(-size.X / 2f, -size.Y / 2f + 4f)]
        };
        body.AddChild(collision);
        body.AddChild(visual);
        body.AddChild(rim);
        parent.AddChild(body);
        return body;
    }

    private static Node2D Backdrop()
    {
        var backdrop = new Node2D { Name = "Backdrop", ZIndex = -10 };
        backdrop.AddChild(new Polygon2D
        {
            Name = "NightWall",
            Color = new Color("111827"),
            Polygon = [new Vector2(-900f, -500f), new Vector2(900f, -500f), new Vector2(900f, 700f), new Vector2(-900f, 700f)]
        });
        backdrop.AddChild(new Polygon2D
        {
            Name = "TowerSilhouette",
            Color = new Color("1d2939"),
            Polygon = [new Vector2(210f, -420f), new Vector2(340f, -420f), new Vector2(340f, 240f), new Vector2(210f, 240f)]
        });
        return backdrop;
    }

    private static ThreadTarget2D ThreadAnchor()
    {
        var target = new ThreadTarget2D { Name = "ThreadAnchor", Position = new Vector2(260f, -80f), Kind = ThreadTargetKind.Anchor };
        var area = new Area2D { Name = "TargetCollision", CollisionLayer = ThreadTargetLayer, CollisionMask = 0, Monitoring = true };
        area.AddChild(new CollisionShape2D { Name = "Collision", Shape = new CircleShape2D { Radius = 22f } });
        target.AddChild(area);
        target.AddChild(new Polygon2D
        {
            Name = "AnchorVisual",
            Color = new Color("6ad6e8"),
            Polygon = [new Vector2(0f, -25f), new Vector2(20f, 0f), new Vector2(0f, 25f), new Vector2(-20f, 0f)]
        });
        return target;
    }

    private static Node2D BellTarget()
    {
        var bell = new Node2D { Name = "BellTarget", Position = new Vector2(450f, -92f) };
        bell.AddChild(new HealthComponent { Name = "Health", MaxHealth = 999, InvulnerabilitySeconds = 0f });
        var hurtbox = new Hurtbox
        {
            Name = "Hurtbox", Monitoring = true, Monitorable = true,
            CollisionLayer = CombatCollisionLayers.Enemy, CollisionMask = 0
        };
        hurtbox.AddChild(new CollisionShape2D { Name = "Collision", Shape = new CircleShape2D { Radius = 18f } });
        bell.AddChild(hurtbox);
        bell.AddChild(new Polygon2D
        {
            Name = "BellVisual", Color = new Color("d9a441"),
            Polygon = [new Vector2(-18f, -16f), new Vector2(18f, -16f), new Vector2(25f, 15f), new Vector2(-25f, 15f)]
        });
        return bell;
    }

    private static Area2D Checkpoint()
    {
        var checkpoint = new Area2D { Name = "Checkpoint", Position = new Vector2(120f, 20f), CollisionLayer = 0, CollisionMask = PlayerLayer, Monitoring = true };
        checkpoint.AddChild(new CollisionShape2D { Name = "Collision", Shape = new RectangleShape2D { Size = new Vector2(40f, 90f) } });
        checkpoint.AddChild(new Polygon2D
        {
            Name = "BeaconVisual", Color = new Color("f3da75"),
            Polygon = [new Vector2(0f, -46f), new Vector2(14f, -20f), new Vector2(0f, 0f), new Vector2(-14f, -20f)]
        });
        return checkpoint;
    }

    private static KillZone KillZone()
    {
        var zone = new KillZone { Name = "KillZone", Position = new Vector2(0f, 560f), CollisionLayer = 0, CollisionMask = PlayerLayer, Monitoring = true };
        zone.AddChild(new CollisionShape2D { Name = "Collision", Shape = new RectangleShape2D { Size = new Vector2(1800f, 80f) } });
        return zone;
    }

    private static Area2D ExitMarker()
    {
        var exit = new Area2D { Name = "ExitMarker", Position = new Vector2(640f, 20f), CollisionLayer = 0, CollisionMask = PlayerLayer, Monitoring = true };
        exit.AddChild(new CollisionShape2D { Name = "Collision", Shape = new RectangleShape2D { Size = new Vector2(34f, 100f) } });
        exit.AddChild(new Polygon2D
        {
            Name = "ExitVisual", Color = new Color("7de2a4"),
            Polygon = [new Vector2(-12f, -50f), new Vector2(12f, -50f), new Vector2(12f, 50f), new Vector2(-12f, 50f)]
        });
        return exit;
    }

    private static PrototypeHud BuildHud()
    {
        var hud = new PrototypeHud { Name = "Hud", Layer = 2 };
        var panel = new ColorRect { Name = "Panel", Position = new Vector2(18f, 16f), Size = new Vector2(680f, 42f), Color = new Color(0.03f, 0.06f, 0.10f, 0.82f) };
        var label = new Label { Name = "Status", Position = new Vector2(30f, 24f), Size = new Vector2(650f, 28f) };
        label.AddThemeColorOverride("font_color", new Color("e8dfc6"));
        hud.AddChild(panel);
        hud.AddChild(label);
        return hud;
    }
}
