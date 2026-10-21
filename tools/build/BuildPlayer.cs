using Godot;
using System;
using System.IO;
using System.Linq;
using Knight.Abilities;
using Knight.Combat;
using Knight.Config;
using Knight.Interactions;
using Knight.Player;
using Knight.Presentation;
using Knight.Tools;
using Knight.Vfx;

namespace Knight.Build;

public partial class BuildPlayer : SceneTree
{
    // Read-only admission shared by the real entry and its negative tests.
    // No invalid/missing argument can fall back to the formal project scene.
    public static string ValidateCandidateOutput(string[] arguments, string projectRoot, string userRoot)
    {
        const string prefix = "--player-output-root=";
        string[] values = arguments.Where(a => a.StartsWith(prefix, StringComparison.Ordinal)).ToArray();
        if (values.Length != 1 || arguments.Any(a => !a.StartsWith(prefix, StringComparison.Ordinal) &&
                !a.StartsWith("--player-module-resource=", StringComparison.Ordinal) &&
                !a.StartsWith("--case-storage-root=", StringComparison.Ordinal)))
            throw new ArgumentException("BuildPlayer requires exactly one --player-output-root and no unknown arguments.");
        string supplied = values[0][prefix.Length..];
        if (!Path.IsPathFullyQualified(supplied)) throw new ArgumentException("Player candidate output must be absolute.");
        string root = Path.GetFullPath(supplied).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        if (root.Equals(Path.GetPathRoot(root)?.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar), StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Player candidate output cannot be a filesystem root.");
        foreach (string protectedPath in new[] { projectRoot, userRoot })
        {
            string protectedRoot = Path.GetFullPath(protectedPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            if (Within(root, protectedRoot) || Within(protectedRoot, root))
                throw new InvalidOperationException("Player candidate overlaps protected project/userdata ancestry.");
        }
        AssertNoReparse(root);
        if (!Directory.Exists(root) || Directory.EnumerateFileSystemEntries(root).Any())
            throw new InvalidOperationException("Player candidate requires an existing empty output directory.");
        string output = Path.Combine(root, "Player.tscn");
        AssertNoReparse(output);
        if (File.Exists(output) || Directory.Exists(output)) throw new InvalidOperationException("Player candidate refuses an existing output.");
        return output;
    }

    private static bool Within(string path, string root) => path.Equals(root, StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);

    private static void AssertNoReparse(string path)
    {
        try
        {
            if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
                throw new InvalidOperationException("Player candidate cannot traverse a reparse leaf.");
        }
        catch (FileNotFoundException) { }
        catch (DirectoryNotFoundException) { }
        for (DirectoryInfo parent = new(path); parent != null; parent = parent.Parent)
            if (parent.Exists && (parent.Attributes & FileAttributes.ReparsePoint) != 0)
                throw new InvalidOperationException("Player candidate cannot traverse a reparse ancestor.");
    }

    public override void _Initialize()
    {
        try
        {
            string output = ValidateCandidateOutput(OS.GetCmdlineUserArgs(), ProjectSettings.GlobalizePath("res://"), OS.GetUserDataDir());
            BuildCandidate(output);
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            Quit(1);
        }
    }

    private void BuildCandidate(string output)
    {
        using MobilityModuleDefinition initialModule = ReadInitialModule(OS.GetCmdlineUserArgs());
        MobilityModuleSettings expectedModule = initialModule.ToSettings();
        // Read the actual current author Resource, not a fabricated default.
        Node sourcePlayer = GD.Load<PackedScene>("res://scenes/player/Player.tscn").Instantiate();
        PlayerTuning authored = sourcePlayer.GetNode<PlayerMotor>("Motor").Tuning;
        var migrationErrors = authored.ValidateThrusterMigration(authored.ResourcePath);
        if (migrationErrors.Count > 0)
        {
            sourcePlayer.Free();
            throw new InvalidOperationException("BuildPlayer unmapped author fields: " + string.Join("; ", migrationErrors));
        }
        using var playerTuning = (PlayerTuning)authored.Duplicate(true);
        sourcePlayer.Free();
        using var energyTuning = new PlayerEnergyTuning();
        using var presentationProfile = new PresentationProfile();
        using var bodyShape = new CapsuleShape2D { Radius = 20f, Height = 72f };
        using var hurtShape = new CapsuleShape2D { Radius = 20f, Height = 72f };
        using var attackShape = new RectangleShape2D { Size = new Vector2(36f, 28f) };
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
        var environmentInteraction = new EnvironmentInteractionController
        {
            Name = "EnvironmentInteraction",
            ProcessPhysicsPriority = -50,
            ZIndex = 10
        };
        var motor = new PlayerMotor
        {
            Name = "Motor",
            Tuning = playerTuning,
            InitialMobilityModule = initialModule,
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
            Shape = hurtShape
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
            Shape = attackShape
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
            Shape = bodyShape
        };
        var camera = new Camera2D { Name = "Camera2D", Enabled = true };

        player.AddChild(input);
        player.AddChild(actionModes);
        player.AddChild(mechanicalClaw);
        player.AddChild(environmentInteraction);
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

        // Recheck immediately before the only write; never overwrite a target.
        ValidateCandidateOutput(OS.GetCmdlineUserArgs(), ProjectSettings.GlobalizePath("res://"), OS.GetUserDataDir());
        Error saveResult = SceneBuildUtil.PackAndSave(player, output);
        int prePackCount = CountNodes(player);
        player.Free();
        if (saveResult != Error.Ok)
        {
            GD.PushError($"BuildPlayer: failed to save Player.tscn: {saveResult}");
            Quit(1);
            return;
        }

        PackedScene packed = ResourceLoader.Load<PackedScene>(output);
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
            copy.GetNodeOrNull<EnvironmentInteractionController>("EnvironmentInteraction") != null &&
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
        MobilityModuleDefinition savedModule = copy.GetNode<PlayerMotor>("Motor").InitialMobilityModule;
        bool hasModule = savedModule != null && savedModule.ToSettings() == expectedModule &&
            savedModule.ResourcePath.Replace('\\', '/').StartsWith(output.Replace('\\', '/') + "::", StringComparison.OrdinalIgnoreCase);
        bool hasDeterministicPhysicsOrder =
            copy.GetNode<PlayerInputReader>("Input").ProcessPhysicsPriority == -300 &&
            copy.GetNode<PlayerActionModeController>("ActionModes").ProcessPhysicsPriority == -200 &&
            copy.GetNode<MechanicalClawController>("MechanicalClaw").ProcessPhysicsPriority == -100 &&
            copy.GetNode<EnvironmentInteractionController>("EnvironmentInteraction").ProcessPhysicsPriority == -50 &&
            copy.GetNode<PlayerMotor>("Motor").ProcessPhysicsPriority == 0;
        bool hasExpectedCombatFilters = generatedHurtbox != null && generatedHitbox != null &&
            generatedHurtbox.CollisionLayer == CombatCollisionLayers.Player &&
            generatedHurtbox.CollisionMask == CombatCollisionLayers.None &&
            generatedHitbox.CollisionLayer == CombatCollisionLayers.PlayerAttack &&
            generatedHitbox.CollisionMask == CombatCollisionLayers.Enemy &&
            generatedHurtbox.DamageReceiverPath == new NodePath("../GuardedDamage");
        copy.Free();
        if (prePackCount != postInstantiateCount || !hasRequiredPaths || !hasExpectedCombatFilters ||
            !hasTuningAndPresentation || !hasDeterministicPhysicsOrder || !hasModule)
        {
            GD.PushError("BuildPlayer: Player scene validation failed.");
            Quit(1);
            return;
        }

        GD.Print($"BuildPlayer: pre-pack node count={prePackCount}; post-instantiate node count={postInstantiateCount}");
        GD.Print("PLAYER_CANDIDATE_BUILT external=true default_cache=true formal_written=false");
        Quit(0);
    }

    private static MobilityModuleDefinition ReadInitialModule(string[] arguments)
    {
        const string prefix = "--player-module-resource=";
        string[] values = arguments.Where(a => a.StartsWith(prefix, StringComparison.Ordinal)).ToArray();
        if (values.Length > 1) throw new ArgumentException("BuildPlayer refuses duplicate module author inputs.");
        if (values.Length == 0) return new MobilityModuleDefinition();
        string supplied = values[0][prefix.Length..];
        if (!Path.IsPathFullyQualified(supplied)) throw new ArgumentException("Module author input must be absolute.");
        string path = Path.GetFullPath(supplied);
        string userdata = Path.GetFullPath(OS.GetUserDataDir()).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        if (Within(path, userdata)) throw new InvalidOperationException("Module author input cannot read application userdata.");
        AssertNoReparse(path);
        if (!File.Exists(path) || !path.EndsWith(".tres", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Module author input must be an existing tres Resource.");
        MobilityModuleDefinition authored = ResourceLoader.Load<MobilityModuleDefinition>(path);
        if (authored == null) throw new InvalidOperationException("Module author input has the wrong Resource type: " + path);
        var errors = authored.Validate();
        if (errors.Count != 0) throw new InvalidOperationException("Invalid module author input " + path + ": " + string.Join("; ", errors));
        var owned = (MobilityModuleDefinition)authored.Duplicate(true);
        if (!string.IsNullOrEmpty(owned.ResourcePath))
        {
            owned.Dispose();
            throw new InvalidOperationException("Player module must embed its own new Resource, not retain an external author path.");
        }
        return owned;
    }

    private static int CountNodes(Node node)
    {
        int count = 1;
        foreach (Node child in node.GetChildren()) count += CountNodes(child);
        return count;
    }
}
