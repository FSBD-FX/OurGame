using Godot;
using Knight.Abilities;
using Knight.Combat;
using Knight.Config;
using Knight.Player;
using Knight.Presentation;
using Knight.Tools;
using Knight.Vfx;

namespace Knight.Build;

public partial class BuildPlayer : SceneTree
{
    public override void _Initialize()
    {
        var playerTuning = new PlayerTuning();
        var energyTuning = new PlayerEnergyTuning();
        var presentationProfile = new PresentationProfile();
        var player = new CharacterBody2D
        {
            Name = "Player",
            CollisionLayer = CombatCollisionLayers.Player,
            CollisionMask = CombatCollisionLayers.World
        };
        var input = new PlayerInputReader { Name = "Input", ProcessPhysicsPriority = -300 };
        var actionModes = new PlayerActionModeController
        {
            Name = "ActionModes",
            ProcessPhysicsPriority = -200
        };
        var mechanicalClaw = new MechanicalClawController
        {
            Name = "MechanicalClaw",
            ProcessPhysicsPriority = -100
        };
        var motor = new PlayerMotor
        {
            Name = "Motor",
            Tuning = playerTuning,
            ProcessPhysicsPriority = 0
        };
        var health = new HealthComponent { Name = "Health" };
        var guardedDamage = new PlayerGuardedDamageReceiver { Name = "GuardedDamage" };
        var hurtbox = new Hurtbox
        {
            Name = "Hurtbox",
            Monitoring = true,
            Monitorable = true,
            CollisionLayer = CombatCollisionLayers.Player,
            CollisionMask = CombatCollisionLayers.None,
            DamageReceiverPath = new NodePath("../GuardedDamage")
        };
        var hurtboxCollider = new CollisionShape2D
        {
            Name = "HurtboxCollider",
            Shape = new CapsuleShape2D { Radius = 20f, Height = 72f }
        };
        var combat = new PlayerCombat { Name = "Combat", Tuning = playerTuning };
        var energy = new PlayerEnergyController { Name = "Energy", Tuning = energyTuning };
        var shield = new PlayerShieldController { Name = "Shield" };
        var hitbox = new Hitbox
        {
            Name = "AttackHitbox",
            Monitoring = false,
            Monitorable = false,
            CollisionLayer = CombatCollisionLayers.PlayerAttack,
            CollisionMask = CombatCollisionLayers.Enemy
        };
        var hitboxCollider = new CollisionShape2D
        {
            Name = "AttackCollider",
            Shape = new RectangleShape2D { Size = new Vector2(36f, 28f) }
        };
        var presentation = new PlayerPresentationCoordinator { Name = "Presentation", Profile = presentationProfile };
        var visualRoot = new Node2D { Name = "VisualRoot" };
        PackedScene formalVisualScene = GD.Load<PackedScene>("res://scenes/presentation/PlayerFormalVisual.tscn");
        if (formalVisualScene == null)
        {
            GD.PushError("BuildPlayer: formal player scene must be built first.");
            Quit(1);
            return;
        }
        PlayerFormalVisual visual = formalVisualScene.Instantiate<PlayerFormalVisual>();
        visual.Name = "Formal";
        var clawVisual = new ThreadShuttleVisual { Name = "MechanicalClaw" };
        var clawLine = new ThreadLineVisual { Name = "Line", ZIndex = -1 };
        var shieldVisual = new PlayerShieldPlaceholderVisual { Name = "Shield", ZIndex = 4 };
        var effects = new Node2D { Name = "Effects" };
        var collider = new CollisionShape2D
        {
            Name = "BodyCollider",
            Shape = new CapsuleShape2D { Radius = 20f, Height = 72f }
        };
        var camera = new Camera2D { Name = "Camera2D", Enabled = true };

        player.AddChild(input);
        player.AddChild(actionModes);
        player.AddChild(mechanicalClaw);
        player.AddChild(motor);
        player.AddChild(health);
        player.AddChild(guardedDamage);
        player.AddChild(hurtbox);
        hurtbox.AddChild(hurtboxCollider);
        player.AddChild(combat);
        combat.AddChild(hitbox);
        hitbox.AddChild(hitboxCollider);
        player.AddChild(shield);
        player.AddChild(energy);
        player.AddChild(presentation);
        player.AddChild(visualRoot);
        visualRoot.AddChild(visual);
        visualRoot.AddChild(clawVisual);
        clawVisual.AddChild(clawLine);
        visualRoot.AddChild(shieldVisual);
        visualRoot.AddChild(effects);
        player.AddChild(collider);
        player.AddChild(camera);

        Error saveResult = SceneBuildUtil.PackAndSave(player, "res://scenes/player/Player.tscn");
        int prePackCount = CountNodes(player);
        player.Free();
        if (saveResult != Error.Ok)
        {
            GD.PushError($"BuildPlayer: failed to save Player.tscn: {saveResult}");
            Quit(1);
            return;
        }

        PackedScene packed = ResourceLoader.Load<PackedScene>("res://scenes/player/Player.tscn");
        Node copy = packed.Instantiate();
        int postInstantiateCount = CountNodes(copy);
        Hurtbox generatedHurtbox = copy.GetNodeOrNull<Hurtbox>("Hurtbox");
        Hitbox generatedHitbox = copy.GetNodeOrNull<Hitbox>("Combat/AttackHitbox");
        bool hasRequiredPaths = copy.GetNodeOrNull<Node>("Input") != null &&
            copy.GetNodeOrNull<PlayerActionModeController>("ActionModes") != null &&
            copy.GetNodeOrNull<Node>("Motor") != null &&
            copy.GetNodeOrNull<Node>("Health") != null &&
            copy.GetNodeOrNull<PlayerGuardedDamageReceiver>("GuardedDamage") != null &&
            copy.GetNodeOrNull<Node>("Hurtbox") != null &&
            copy.GetNodeOrNull<Node>("Hurtbox/HurtboxCollider") != null &&
            copy.GetNodeOrNull<Node>("Combat") != null &&
            copy.GetNodeOrNull<Node>("Combat/AttackHitbox") != null &&
            copy.GetNodeOrNull<Node>("Combat/AttackHitbox/AttackCollider") != null &&
            copy.GetNodeOrNull<MechanicalClawController>("MechanicalClaw") != null &&
            copy.GetNodeOrNull<ThreadAbility>("ThreadAbility") == null &&
            copy.GetNodeOrNull<PlayerEnergyController>("Energy") != null &&
            copy.GetNodeOrNull<PlayerShieldController>("Shield") != null &&
            copy.GetNodeOrNull<PlayerPresentationCoordinator>("Presentation") != null &&
            copy.GetNodeOrNull<Node2D>("VisualRoot") != null &&
            copy.GetNodeOrNull<PlayerFormalVisual>("VisualRoot/Formal") is IEntityVisual2D &&
            copy.GetNodeOrNull<ThreadShuttleVisual>("VisualRoot/MechanicalClaw") != null &&
            copy.GetNodeOrNull<ThreadLineVisual>("VisualRoot/MechanicalClaw/Line") != null &&
            copy.GetNodeOrNull<PlayerShieldPlaceholderVisual>("VisualRoot/Shield") != null &&
            copy.GetNodeOrNull<Node>("VisualRoot/Shield") is not CollisionObject2D &&
            copy.FindChildren("*", "PlayerPlaceholderVisual", true, false).Count == 0 &&
            copy.GetNodeOrNull<Node2D>("VisualRoot/Effects") != null &&
            copy.GetNodeOrNull<Node>("BodyCollider") != null;
        bool hasTuningAndPresentation = copy.GetNode<PlayerMotor>("Motor").Tuning != null &&
            copy.GetNode<PlayerCombat>("Combat").Tuning != null &&
            copy.GetNode<PlayerEnergyController>("Energy").Tuning != null &&
            copy.GetNode<PlayerPresentationCoordinator>("Presentation").Profile != null;
        bool hasDeterministicPhysicsOrder =
            copy.GetNode<PlayerInputReader>("Input").ProcessPhysicsPriority == -300 &&
            copy.GetNode<PlayerActionModeController>("ActionModes").ProcessPhysicsPriority == -200 &&
            copy.GetNode<MechanicalClawController>("MechanicalClaw").ProcessPhysicsPriority == -100 &&
            copy.GetNode<PlayerMotor>("Motor").ProcessPhysicsPriority == 0;
        bool hasExpectedCombatFilters = generatedHurtbox != null && generatedHitbox != null &&
            generatedHurtbox.CollisionLayer == CombatCollisionLayers.Player &&
            generatedHurtbox.CollisionMask == CombatCollisionLayers.None &&
            generatedHitbox.CollisionLayer == CombatCollisionLayers.PlayerAttack &&
            generatedHitbox.CollisionMask == CombatCollisionLayers.Enemy &&
            generatedHurtbox.DamageReceiverPath == new NodePath("../GuardedDamage");
        copy.Free();
        if (prePackCount != postInstantiateCount || !hasRequiredPaths || !hasExpectedCombatFilters ||
            !hasTuningAndPresentation || !hasDeterministicPhysicsOrder)
        {
            GD.PushError("BuildPlayer: Player scene validation failed.");
            Quit(1);
            return;
        }

        GD.Print($"BuildPlayer: pre-pack node count={prePackCount}; post-instantiate node count={postInstantiateCount}");
        Quit(0);
    }

    private static int CountNodes(Node node)
    {
        int count = 1;
        foreach (Node child in node.GetChildren()) count += CountNodes(child);
        return count;
    }
}
