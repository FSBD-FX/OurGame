using System;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using Knight.Abilities;
using Knight.Combat;
using Knight.Enemies.Moth;
using Knight.Player;
using Knight.Presentation;
using Knight.Shared;

namespace Knight.Tests.Integration;

public static class GraveLampMothRuntimeTests
{
    private const string MothScenePath = "res://scenes/enemies/GraveLampMoth.tscn";
    private const float AllowableHorizontalError = .001f;

    public static async Task Run(SceneTree tree)
    {
        FormalScenePreservesGameplayAndReplaceableVisualContracts();
        await RealPhysicsLocksSweptPathThroughLandingAndRecovery(tree);
        await RealPhysicsClampsLockedPathToWorldCollision(tree);
        await RealPhysicsStopsAtLockedFirstContactOnDiagonalWorldCollision(tree);
        await RealPhysicsTelegraphsDivesDamagesAndRecovers(tree);
        await RealMechanicalClawInputPullsMothDownStunsLandsAndResetRestoresSpawn(tree);
    }

    private static async Task RealPhysicsLocksSweptPathThroughLandingAndRecovery(SceneTree tree)
    {
        var root = new Node2D { Name = "GraveLampMothLockedPathFixture" };
        var player = new Node2D { Name = "Player", Position = new Vector2(0f, 140f) };
        player.AddToGroup("player");
        GraveLampMoth moth = GD.Load<PackedScene>(MothScenePath).Instantiate<GraveLampMoth>();
        moth.Name = "GraveLampMoth";
        root.AddChild(player);
        root.AddChild(moth);
        tree.Root.AddChild(root);
        AttackPresentationFrame published = default;
        moth.AttackFrameChanged += frame => published = frame;
        await Physics(tree);

        for (int i = 0; i < 90 &&
            !(published.Timeline.Phase == AttackPhase.Telegraph && published.Timeline.HasLockedTarget); i++)
            await Physics(tree);
        TestAssert.True(published.Timeline.Phase == AttackPhase.Telegraph && published.Timeline.HasLockedTarget,
            "real-physics Moth reaches its locked Telegraph before Active");
        TestAssert.Equal(CombatCollisionLayers.World, moth.CollisionMask,
            "unobstructed Moth fixture preserves the packed scene production World collision mask");
        CollisionShape2D collider = moth.GetNode<CollisionShape2D>("AttackHitbox/AttackCollider");
        float radius = ((CircleShape2D)collider.Shape).Radius;
        Vector2 lockedStart = collider.GlobalPosition;
        Vector2 lockedTarget = published.Timeline.LockedWorldTarget;
        float lockedDistance = lockedStart.DistanceTo(lockedTarget);
        TestAssert.Equal(AttackShapeKind.Capsule, published.Shape.Kind,
            "real-physics locked Moth warning is the swept circle capsule");
        TestAssert.True(published.Shape.Origin.DistanceTo((lockedStart + lockedTarget) * .5f) < .01f &&
            published.Shape.Extent.DistanceTo(new Vector2(radius, lockedDistance * .5f + radius)) < .01f,
            "real-physics Moth capsule derives from locked endpoints and the real collider radius");
        AttackShapeFrame lockedPath = published.Shape;

        player.Position = new Vector2(230f, 140f);
        await Physics(tree);
        await Physics(tree);
        TestAssert.Equal(AttackPhase.Telegraph, published.Timeline.Phase,
            "crossing the Moth path leaves time in the locked Telegraph window");
        TestAssert.True(published.Timeline.LockedWorldTarget.DistanceTo(lockedTarget) < .01f &&
            published.Shape.Origin.DistanceTo(lockedPath.Origin) < .01f &&
            published.Shape.Extent.DistanceTo(lockedPath.Extent) < .01f,
            "real-physics player crossover cannot move the warned Moth trajectory or landing");

        for (int i = 0; i < 60 && published.Timeline.Phase != AttackPhase.Active; i++) await Physics(tree);
        TestAssert.Equal(AttackPhase.Active, published.Timeline.Phase,
            "real-physics Moth reaches Active on its locked trajectory");
        int activeFrames = 0;
        while (activeFrames < 30 && published.Timeline.Phase == AttackPhase.Active)
        {
            TestAssert.Equal(AttackShapeKind.Circle, published.Shape.Kind,
                "every real-physics Moth Active frame publishes the current circular collider");
            TestAssert.True(published.Shape.Origin.DistanceTo(collider.GlobalPosition) < .01f &&
                published.Shape.Extent.DistanceTo(Vector2.One * radius) < .01f,
                "every real-physics Moth Active frame is published after movement from the real collider transform");
            activeFrames++;
            await Physics(tree);
        }
        TestAssert.True(activeFrames > 1, "real-physics Moth exposes a dense multi-frame Active traversal");
        TestAssert.Equal(AttackPhase.Recovery, published.Timeline.Phase,
            "real-physics Moth closes its locked dive into Recovery");
        TestAssert.True(collider.GlobalPosition.DistanceTo(lockedTarget) < .1f,
            $"real-physics Moth lands at the warned endpoint; expected={lockedTarget}, actual={collider.GlobalPosition}");
        AttackTelegraphVisual telegraph = moth.GetNode<AttackTelegraphVisual>("VisualRoot/AttackTelegraph");
        TestAssert.True(!moth.GetNode<Hitbox>("AttackHitbox").Monitoring && !telegraph.GoldVisible &&
            !telegraph.DangerVisible && !telegraph.ImpactVisible,
            "real-physics Moth Recovery clears damage, warning, and impact layers");
        await Dispose(tree, root);
    }

    private static async Task RealPhysicsClampsLockedPathToWorldCollision(SceneTree tree)
    {
        const float obstacleCenterY = 100f;
        const float obstacleHeight = 20f;
        var root = new Node2D { Name = "GraveLampMothWorldClampFixture" };
        var obstacle = new StaticBody2D
        {
            Name = "WorldObstacle",
            Position = new Vector2(0f, obstacleCenterY),
            CollisionLayer = CombatCollisionLayers.World,
            CollisionMask = CombatCollisionLayers.None
        };
        obstacle.AddChild(new CollisionShape2D
        {
            Name = "Collision",
            Shape = new RectangleShape2D { Size = new Vector2(200f, obstacleHeight) }
        });
        var player = new Node2D { Name = "Player", Position = new Vector2(0f, 140f) };
        player.AddToGroup("player");
        GraveLampMoth moth = GD.Load<PackedScene>(MothScenePath).Instantiate<GraveLampMoth>();
        moth.Name = "GraveLampMoth";
        root.AddChild(obstacle);
        root.AddChild(player);
        root.AddChild(moth);
        tree.Root.AddChild(root);
        AttackPresentationFrame published = default;
        moth.AttackFrameChanged += frame => published = frame;
        await Physics(tree);

        for (int i = 0; i < 90 &&
            !(published.Timeline.Phase == AttackPhase.Telegraph && published.Timeline.HasLockedTarget); i++)
            await Physics(tree);
        TestAssert.True(published.Timeline.Phase == AttackPhase.Telegraph && published.Timeline.HasLockedTarget,
            "production-collision Moth reaches a locked warning before the blocked dive");
        TestAssert.Equal(CombatCollisionLayers.World, moth.CollisionMask,
            "blocked Moth fixture preserves the packed scene production World collision mask");
        CollisionShape2D attackCollider = moth.GetNode<CollisionShape2D>("AttackHitbox/AttackCollider");
        float attackRadius = ((CircleShape2D)attackCollider.Shape).Radius;
        Vector2 lockedStart = attackCollider.GlobalPosition;
        Vector2 lockedTarget = published.Timeline.LockedWorldTarget;
        AttackShapeFrame lockedPath = published.Shape;
        float lockedDistance = lockedStart.DistanceTo(lockedTarget);
        TestAssert.Equal(AttackShapeKind.Capsule, lockedPath.Kind,
            "blocked Moth warning remains the real attack circle swept to its clamped endpoint");
        TestAssert.True(lockedPath.Origin.DistanceTo((lockedStart + lockedTarget) * .5f) < .01f &&
            lockedPath.Extent.DistanceTo(new Vector2(attackRadius, lockedDistance * .5f + attackRadius)) < .01f,
            "blocked Moth warning bounds derive from clamped endpoints and the real attack radius");
        float obstacleFarSide = obstacleCenterY + obstacleHeight * .5f;
        bool warningCrossesObstacle = lockedPath.Origin.Y + lockedPath.Extent.Y > obstacleFarSide;

        player.Position = new Vector2(230f, 140f);
        await Physics(tree);
        await Physics(tree);
        TestAssert.True(published.Timeline.LockedWorldTarget.DistanceTo(lockedTarget) < .01f &&
            published.Shape.Origin.DistanceTo(lockedPath.Origin) < .01f &&
            published.Shape.Extent.DistanceTo(lockedPath.Extent) < .01f,
            "production-collision Moth clamp remains locked when the player crosses");

        for (int i = 0; i < 60 && published.Timeline.Phase != AttackPhase.Active; i++) await Physics(tree);
        int activeFrames = 0;
        while (activeFrames < 30 && published.Timeline.Phase == AttackPhase.Active)
        {
            TestAssert.Equal(AttackShapeKind.Circle, published.Shape.Kind,
                "blocked Moth Active publishes its current real circle on every physics frame");
            TestAssert.True(published.Shape.Origin.DistanceTo(attackCollider.GlobalPosition) < .01f &&
                published.Shape.Extent.DistanceTo(Vector2.One * attackRadius) < .01f,
                "blocked Moth Active circle equals its post-movement real collider");
            activeFrames++;
            await Physics(tree);
        }
        TestAssert.True(activeFrames > 1, "blocked Moth still exposes a multi-frame Active traversal");
        TestAssert.Equal(AttackPhase.Recovery, published.Timeline.Phase,
            "real World contact moves the blocked Moth into Recovery");
        Vector2 recoveryPosition = attackCollider.GlobalPosition;
        AttackTelegraphVisual telegraph = moth.GetNode<AttackTelegraphVisual>("VisualRoot/AttackTelegraph");
        bool recoveryClear = !moth.GetNode<Hitbox>("AttackHitbox").Monitoring &&
            !telegraph.GoldVisible && !telegraph.DangerVisible && !telegraph.ImpactVisible;
        TestAssert.True(!warningCrossesObstacle && recoveryPosition.DistanceTo(lockedTarget) < .001f && recoveryClear,
            $"production-collision Moth warning clamps to its reachable World-contact endpoint; " +
            $"locked={lockedTarget}, recovery={recoveryPosition}, warning_end=" +
            $"{lockedPath.Origin.Y + lockedPath.Extent.Y:0.###}, obstacle_far={obstacleFarSide:0.###}");
        await Dispose(tree, root);
    }

    private static async Task RealPhysicsStopsAtLockedFirstContactOnDiagonalWorldCollision(SceneTree tree)
    {
        var root = new Node2D { Name = "GraveLampMothDiagonalWorldFixture" };
        var platform = new StaticBody2D
        {
            Name = "DiagonalWorldPlatform",
            Position = new Vector2(0f, 100f),
            Rotation = Mathf.DegToRad(65f),
            CollisionLayer = CombatCollisionLayers.World,
            CollisionMask = CombatCollisionLayers.None
        };
        platform.AddChild(new CollisionShape2D
        {
            Name = "Collision",
            Shape = new RectangleShape2D { Size = new Vector2(220f, 20f) }
        });
        var player = new Node2D { Name = "Player", Position = new Vector2(0f, 140f) };
        player.AddToGroup("player");
        GraveLampMoth moth = GD.Load<PackedScene>(MothScenePath).Instantiate<GraveLampMoth>();
        moth.Name = "GraveLampMoth";
        root.AddChild(platform);
        root.AddChild(player);
        root.AddChild(moth);
        tree.Root.AddChild(root);
        AttackPresentationFrame published = default;
        moth.AttackFrameChanged += frame => published = frame;
        await Physics(tree);

        for (int i = 0; i < 90 &&
            !(published.Timeline.Phase == AttackPhase.Telegraph && published.Timeline.HasLockedTarget); i++)
            await Physics(tree);
        TestAssert.True(published.Timeline.Phase == AttackPhase.Telegraph && published.Timeline.HasLockedTarget,
            "diagonal production collision reaches a locked Moth warning");
        TestAssert.Equal(CombatCollisionLayers.World, moth.CollisionMask,
            "diagonal Moth fixture preserves the packed scene production World collision mask");
        CollisionShape2D attackCollider = moth.GetNode<CollisionShape2D>("AttackHitbox/AttackCollider");
        float attackRadius = ((CircleShape2D)attackCollider.Shape).Radius;
        Vector2 lockedTarget = published.Timeline.LockedWorldTarget;
        AttackShapeFrame lockedPath = published.Shape;

        player.Position = new Vector2(230f, 140f);
        await Physics(tree);
        await Physics(tree);
        TestAssert.True(published.Timeline.LockedWorldTarget.DistanceTo(lockedTarget) < AllowableHorizontalError &&
            published.Shape.Origin.DistanceTo(lockedPath.Origin) < AllowableHorizontalError,
            "player crossover cannot retarget the diagonal first-contact warning");

        for (int i = 0; i < 60 && published.Timeline.Phase != AttackPhase.Active; i++) await Physics(tree);
        int activeFrames = 0;
        while (activeFrames < 30 && published.Timeline.Phase == AttackPhase.Active)
        {
            TestAssert.Equal(AttackShapeKind.Circle, published.Shape.Kind,
                "diagonal Moth Active publishes a current circle on every physics frame");
            TestAssert.True(published.Shape.Origin.DistanceTo(attackCollider.GlobalPosition) < .01f &&
                published.Shape.Extent.DistanceTo(Vector2.One * attackRadius) < .01f,
                "diagonal Moth Active circle equals its post-movement real collider");
            activeFrames++;
            await Physics(tree);
        }
        TestAssert.True(activeFrames > 1, "diagonal Moth exposes a multi-frame Active traversal");
        TestAssert.Equal(AttackPhase.Recovery, published.Timeline.Phase,
            "diagonal World contact moves the Moth into Recovery");
        Vector2 recoveryPosition = attackCollider.GlobalPosition;
        float horizontalError = Mathf.Abs(recoveryPosition.X - lockedTarget.X);
        float endpointError = recoveryPosition.DistanceTo(lockedTarget);
        TestAssert.True(horizontalError <= AllowableHorizontalError && endpointError <= AllowableHorizontalError,
            $"diagonal production collision stops the Moth at its locked first-contact endpoint; " +
            $"locked={lockedTarget}, recovery={recoveryPosition}, horizontal_error={horizontalError:0.######}, " +
            $"endpoint_error={endpointError:0.######}");
        await Dispose(tree, root);
    }

    private static void FormalScenePreservesGameplayAndReplaceableVisualContracts()
    {
        TestAssert.True(ResourceLoader.Exists(MothScenePath), "formal Grave-Lamp Moth packed scene exists");
        GraveLampMoth moth = GD.Load<PackedScene>(MothScenePath).Instantiate<GraveLampMoth>();
        TestAssert.True(moth.GetNodeOrNull<HealthComponent>("Health") != null,
            "formal moth exposes stable Health path");
        TestAssert.True(moth.GetNodeOrNull<Hurtbox>("Hurtbox") != null,
            "formal moth exposes stable Hurtbox path");
        TestAssert.True(moth.GetNodeOrNull<Hitbox>("AttackHitbox") != null,
            "formal moth exposes stable AttackHitbox path");
        ThreadTarget2D target = moth.GetNode<ThreadTarget2D>("ThreadTarget");
        TestAssert.Equal(ThreadTargetKind.LightEnemy, target.Kind,
            "formal moth remains target-agnostic light enemy thread target");
        TestAssert.Equal(new NodePath(".."), target.PullReceiverPath,
            "formal moth thread target resolves the pull receiver through a stable path");
        TestAssert.True(moth.GetNodeOrNull<GraveLampMothPresentationCoordinator>("Presentation") != null,
            "formal moth exposes stable Presentation path");
        var visual = moth.GetNode<GraveLampMothFormalVisual>("VisualRoot/Formal");
        TestAssert.True(visual is IEntityVisual2D,
            "formal moth implements the common replaceable visual contract");
        TestAssert.True(moth.GetNodeOrNull<Node2D>("VisualRoot/Effects") != null,
            "formal moth exposes a stable effects mount");
        TestAssert.Equal(2, moth.GetNode<HealthComponent>("Health").MaxHealth,
            "formal moth health consumes its authored tuning");

        visual.ApplyMothFrame(new GraveLampMothFrame(GraveLampMothState.Telegraph, 0f, 0f, false, true), 1, false);
        TestAssert.Equal("telegraph", visual.CurrentAnimation,
            "moth telegraph uses a named silhouette state without relying on color");
        visual.ApplyMothFrame(new GraveLampMothFrame(GraveLampMothState.Dive, 0f, 420f, true, true), 1, false);
        TestAssert.Equal("dive", visual.CurrentAnimation,
            "moth dive uses a distinct axis-oriented pose");
        visual.ApplyMothFrame(new GraveLampMothFrame(GraveLampMothState.ThreadStunned, 0f, 0f, false, true), 1, false);
        TestAssert.Equal("thread_pull", visual.CurrentAnimation,
            "thread-stunned moth uses a collapsed named pose");
        TestAssert.Equal(Vector2.One, visual.Scale,
            "formal moth preserves uniform scale instead of stretching the complete sprite");
        moth.Free();
    }

    private static async Task RealPhysicsTelegraphsDivesDamagesAndRecovers(SceneTree tree)
    {
        RuntimeFixture fixture = await CreateFixture(tree, new Vector2(0f, -16f));
        int before = fixture.Player.GetNode<HealthComponent>("Health").CurrentHealth;
        bool sawTelegraph = false;
        bool sawDive = false;
        float telegraphSpeed = float.PositiveInfinity;
        for (int i = 0; i < 90; i++)
        {
            await Physics(tree);
            if (fixture.Moth.CurrentState == GraveLampMothState.Telegraph)
            {
                sawTelegraph = true;
                telegraphSpeed = Mathf.Min(telegraphSpeed, fixture.Moth.Velocity.Length());
            }
            if (fixture.Moth.CurrentState == GraveLampMothState.Dive)
            {
                sawDive = true;
                break;
            }
        }

        TestAssert.True(sawTelegraph, "real moth acquires the player and visibly telegraphs before diving");
        TestAssert.True(telegraphSpeed < 1f, "real telegraph holds position instead of dealing movement damage");
        TestAssert.True(sawDive, "real moth reaches dive state through ordinary physics frames");
        TestAssert.True(fixture.Moth.Velocity.Y > 400f,
            "real dive uses the configured short downward dash speed");

        bool damaged = false;
        bool recovered = false;
        for (int i = 0; i < 60; i++)
        {
            await Physics(tree);
            damaged |= fixture.Player.GetNode<HealthComponent>("Health").CurrentHealth < before;
            recovered |= fixture.Moth.CurrentState == GraveLampMothState.Recover;
            if (damaged && recovered) break;
        }
        TestAssert.True(damaged, "real moth Hitbox damages the formal player during its active dive");
        TestAssert.True(recovered, "real world contact or dive duration closes damage into recovery");
        TestAssert.True(!fixture.Moth.GetNode<Hitbox>("AttackHitbox").Monitoring,
            "recovery disables the formal attack hitbox");
        await Dispose(tree, fixture.Root);
    }

    private static async Task RealMechanicalClawInputPullsMothDownStunsLandsAndResetRestoresSpawn(
        SceneTree tree)
    {
        Vector2 authoredSpawn = new(180f, -56f);
        RuntimeFixture fixture = await CreateFixture(tree, authoredSpawn);
        PlayerInputReader input = fixture.Player.GetNode<PlayerInputReader>("Input");
        PlayerActionModeController modes = fixture.Player.GetNode<PlayerActionModeController>("ActionModes");
        MechanicalClawController claw = fixture.Player.GetNode<MechanicalClawController>("MechanicalClaw");
        GrappleSurface2D target = fixture.Moth.GetNode<GrappleSurface2D>("ThreadTarget");
        claw.ResetTransientState();
        input.SetPhysicsProcess(false);
        modes.SetPhysicsProcess(false);
        claw.SetPhysicsProcess(false);

        Input.ActionPress(InputActions.SelectGrappleMode);
        Input.ActionPress(InputActions.PrimaryAction);
        for (int frame = 0; frame < 120 &&
            fixture.Moth.CurrentState != GraveLampMothState.ThreadStunned; frame++)
        {
            SampleClawInput(input, modes, claw, target.WorldAnchor);
            await Physics(tree);
            if (frame == 0) Input.ActionRelease(InputActions.SelectGrappleMode);
        }
        Input.ActionRelease(InputActions.PrimaryAction);
        input.SetPhysicsProcess(true);
        modes.SetPhysicsProcess(true);
        claw.SetPhysicsProcess(true);
        await WaitForMothState(tree, fixture.Moth, GraveLampMothState.ThreadStunned,
            "real mechanical claw cast to reach its legacy-adapter apply result");

        TestAssert.Equal(GraveLampMothState.ThreadStunned, fixture.Moth.CurrentState,
            "real aim plus thread input interrupts the moth into ThreadStunned");
        TestAssert.True(fixture.Moth.Velocity.X < -150f && fixture.Moth.Velocity.Y > 150f,
            "real light-enemy thread pull moves the moth down and toward the player");

        bool landed = false;
        for (int i = 0; i < 60; i++)
        {
            await Physics(tree);
            if (fixture.Moth.IsOnFloor())
            {
                landed = true;
                break;
            }
        }
        TestAssert.True(landed, "thread-stunned moth falls onto real world collision before recovering");

        fixture.Moth.GlobalPosition = new Vector2(330f, -120f);
        fixture.Moth.Velocity = new Vector2(240f, -180f);
        fixture.Moth.GetNode<HealthComponent>("Health").ApplyDamage(
            new DamageRequest(1, Vector2.Right, 40f, 8501UL));
        fixture.Moth.ResetForCheckpoint();
        TestAssert.True(fixture.Moth.GlobalPosition.DistanceTo(authoredSpawn) < 0.1f,
            "checkpoint reset restores the authored moth spawn");
        TestAssert.Equal(Vector2.Zero, fixture.Moth.Velocity,
            "checkpoint reset clears moth dive and pull velocity");
        TestAssert.Equal(GraveLampMothState.Hover, fixture.Moth.CurrentState,
            "checkpoint reset restores the moth state machine to Hover");
        TestAssert.Equal(fixture.Moth.GetNode<HealthComponent>("Health").MaxHealth,
            fixture.Moth.GetNode<HealthComponent>("Health").CurrentHealth,
            "checkpoint reset restores moth health");
        await Dispose(tree, fixture.Root);
    }

    private static void SampleClawInput(PlayerInputReader input, PlayerActionModeController modes,
        MechanicalClawController claw, Vector2 mouseWorld)
    {
        input._PhysicsProcess(0d);
        PlayerInputFrame frame = input.CurrentFrame with { MouseWorld = mouseWorld };
        typeof(PlayerInputReader).GetProperty(nameof(PlayerInputReader.CurrentFrame),
            BindingFlags.Instance | BindingFlags.Public)!.SetValue(input, frame);
        modes._PhysicsProcess(0d);
        claw._PhysicsProcess(1d / 60d);
    }

    private static async Task<RuntimeFixture> CreateFixture(SceneTree tree, Vector2 mothPosition)
    {
        var root = new Node2D { Name = "GraveLampMothRuntimeFixture" };
        var floor = new StaticBody2D
        {
            Name = "Floor",
            Position = new Vector2(0f, 180f),
            CollisionLayer = CombatCollisionLayers.World,
            CollisionMask = CombatCollisionLayers.None
        };
        floor.AddChild(new CollisionShape2D
        {
            Name = "Collision",
            Shape = new RectangleShape2D { Size = new Vector2(900f, 40f) }
        });
        CharacterBody2D player = GD.Load<PackedScene>("res://scenes/player/Player.tscn")
            .Instantiate<CharacterBody2D>();
        player.Name = "Player";
        player.Position = new Vector2(0f, 124f);
        player.GetNode<Camera2D>("Camera2D").Enabled = false;
        player.AddToGroup("player");
        GraveLampMoth moth = GD.Load<PackedScene>(MothScenePath).Instantiate<GraveLampMoth>();
        moth.Name = "GraveLampMoth";
        moth.Position = mothPosition;
        root.AddChild(floor);
        root.AddChild(player);
        root.AddChild(moth);
        tree.Root.AddChild(root);
        await Physics(tree);
        await Physics(tree);
        return new RuntimeFixture(root, player, moth);
    }

    private static async Task Physics(SceneTree tree) =>
        await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);

    private static async Task WaitForMothState(SceneTree tree, GraveLampMoth moth,
        GraveLampMothState expected, string operation)
    {
        for (int index = 0; index < 120 && moth.CurrentState != expected; index++) await Physics(tree);
        if (moth.CurrentState != expected)
            throw new InvalidOperationException($"Timed out waiting for {operation}; current={moth.CurrentState}");
    }

    private static async Task Dispose(SceneTree tree, Node root)
    {
        Input.ActionRelease(InputActions.MoveRight);
        Input.ActionRelease(InputActions.MoveUp);
        Input.ActionRelease(InputActions.SelectGrappleMode);
        Input.ActionRelease(InputActions.PrimaryAction);
        root.QueueFree();
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
    }

    private sealed record RuntimeFixture(Node2D Root, CharacterBody2D Player, GraveLampMoth Moth);
}
