using System;
using System.Threading.Tasks;
using Godot;
using Knight.Abilities;
using Knight.Audio;
using Knight.Combat;
using Knight.Enemies.Skitter;
using Knight.Player;
using Knight.Shared;
using Knight.Vfx;

namespace Knight.Tests.Integration;

public static class CanyonSkitterRuntimeTests
{
    public static async Task Run(SceneTree tree)
    {
        TestAssert.True(ResourceLoader.Exists("res://scenes/enemies/CanyonSkitter.tscn"),
            "generated Canyon Skitter scene exists before runtime contracts");
        CanyonSkitter skitter = GD.Load<PackedScene>("res://scenes/enemies/CanyonSkitter.tscn")
            .Instantiate<CanyonSkitter>();
        try
        {
            TestAssert.Equal(CombatCollisionLayers.Enemy, skitter.CollisionLayer,
                "body uses enemy layer");
            TestAssert.Equal(CombatCollisionLayers.World, skitter.CollisionMask,
                "body physically collides with world");
            TestAssert.Equal(2, skitter.GetNode<HealthComponent>("Health").MaxHealth,
                "light enemy has two health");
            TestAssert.Equal(ThreadTargetKind.LightEnemy,
                skitter.GetNode<ThreadTarget2D>("ThreadTarget").Kind,
                "claw resolves a light-enemy target");
            TestAssert.True(skitter.GetNodeOrNull<Hitbox>("RushHitbox") != null,
                "one front hitbox owns real contact damage");
        }
        finally { skitter.Free(); }
        await AirborneSkitterCannotWarnUntilBodyLands(tree);
        await GroundRushAndRetreatRemainOnPerch(tree);
        await LeftwardOpenFloorRushAndRetreatAreBounded(tree);
        await ThinWallAndSplitPerchStopBothDirections(tree);
        await DeadHealthlessAndStackedTargetsCannotAggro(tree);
        await ThreadCancelsEachAttackPhaseImmediately(tree);
        await RuntimeCrossBehindAndInvalidDeltasStaySafe(tree);
        await RealDirectionalShieldConsumesOneContact(tree);
        await PullFallLandingAndFallOutPreserveHealthSemantics(tree);
        await PullVelocityIsBoundedAndDoesNotAccumulateEveryFrame(tree);
        await PausedWarningResumesWithoutSkippingRush(tree);
        await LateSpawnResetAndRemovalClearAttack(tree);
        await NativeVisualAndPresentationAreReplaceable(tree);
    }

    // Catches replacing contact-level eligibility with the longer edge-lookahead rays.
    private static async Task AirborneSkitterCannotWarnUntilBodyLands(SceneTree tree)
    {
        var root = new Node2D { Name = "SkitterAirborneStart" };
        AddFloor(root, "Floor", new Vector2(300f, 220f), new Vector2(600f, 20f));
        CanyonSkitter skitter = NewSkitter(new Vector2(300f, 200f));
        root.AddChild(skitter);
        root.AddChild(LivingTarget(new Vector2(390f, 200f)));
        tree.Root.AddChild(root);
        try
        {
            await Frames(tree, 1);
            TestAssert.True(!skitter.IsOnFloor() && skitter.GlobalPosition.Y < 209f,
                "fixture starts with a real gap below the collider, before landing");
            TestAssert.Equal(CanyonSkitterState.Idle, skitter.CurrentFrame.State,
                "an airborne skitter cannot announce a grounded rush before contact");
            await Until(tree, () => skitter.CurrentFrame.State == CanyonSkitterState.Telegraph, 80);
            TestAssert.True(skitter.IsOnFloor() && skitter.GlobalPosition.Y >= 210f,
                "the warning begins only after actual body-floor contact");
        }
        finally { root.QueueFree(); await Frames(tree, 2); }
    }

    private static async Task GroundRushAndRetreatRemainOnPerch(SceneTree tree)
    {
        var root = new Node2D { Name = "SkitterBroadFloor" };
        var floor = new StaticBody2D
        {
            Name = "Floor", Position = new Vector2(300f, 220f),
            CollisionLayer = CombatCollisionLayers.World, CollisionMask = 0
        };
        floor.AddChild(new CollisionShape2D
        {
            Shape = new RectangleShape2D { Size = new Vector2(600f, 20f) }
        });
        root.AddChild(floor);
        CanyonSkitter skitter = GD.Load<PackedScene>("res://scenes/enemies/CanyonSkitter.tscn")
            .Instantiate<CanyonSkitter>();
        root.AddChild(skitter);
        skitter.GlobalPosition = new Vector2(300f, 200f);
        var player = new Node2D { Name = "LivingPlayer", Position = new Vector2(400f, 200f) };
        player.AddToGroup("player");
        player.AddChild(new HealthComponent { Name = "Health", MaxHealth = 5 });
        root.AddChild(player);
        tree.Root.AddChild(root);
        try
        {
            float maximum = skitter.GlobalPosition.X;
            float retreatMinimum = float.PositiveInfinity;
            bool sawTelegraph = false;
            bool sawRush = false;
            bool sawRetreat = false;
            bool sawRecovery = false;
            for (int frame = 0; frame < 180; frame++)
            {
                await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
                CanyonSkitterState state = skitter.CurrentFrame.State;
                sawTelegraph |= state == CanyonSkitterState.Telegraph;
                sawRush |= state == CanyonSkitterState.Rush;
                sawRetreat |= state == CanyonSkitterState.Retreat;
                sawRecovery |= state == CanyonSkitterState.Recover;
                maximum = MathF.Max(maximum, skitter.GlobalPosition.X);
                if (sawRush && state == CanyonSkitterState.Retreat)
                    retreatMinimum = MathF.Min(retreatMinimum, skitter.GlobalPosition.X);
                if (state != CanyonSkitterState.Rush)
                    TestAssert.True(!skitter.GetNode<Hitbox>("RushHitbox").Monitoring,
                        "only the moving front rush can monitor player contact");
            }
            TestAssert.True(sawTelegraph && sawRush && sawRetreat && sawRecovery,
                "real actor must warn, move forward, yield ground, and pause");
            TestAssert.True(maximum > 300f + 20f && maximum <= 300f + 64.5f,
                "body travels a real bounded short rush, not a static thrust");
            TestAssert.True(retreatMinimum < maximum - 12f,
                "retreat moves the body backward rather than stretching the drawing");
            TestAssert.True(MathF.Abs(skitter.GlobalPosition.Y - 211f) < 3f,
                $"grounded sequence remains on the authored floor; actual_y={skitter.GlobalPosition.Y}");
        }
        finally
        {
            root.QueueFree();
            await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
            await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
        }
    }

    // Catches a direction-specific movement branch that only works to the right.
    private static async Task LeftwardOpenFloorRushAndRetreatAreBounded(SceneTree tree)
    {
        var root = new Node2D { Name = "SkitterLeftOpenFloor" };
        AddFloor(root, "Floor", new Vector2(350f, 220f), new Vector2(600f, 20f));
        CanyonSkitter skitter = NewSkitter(new Vector2(400f, 200f));
        root.AddChild(skitter);
        root.AddChild(LivingTarget(new Vector2(300f, 200f)));
        tree.Root.AddChild(root);
        try
        {
            await Until(tree, () => skitter.CurrentFrame.State == CanyonSkitterState.Telegraph, 80);
            float minimum = skitter.GlobalPosition.X;
            float retreatMaximum = float.NegativeInfinity;
            bool sawRush = false;
            bool sawRetreat = false;
            bool sawRecover = false;
            for (int frame = 0; frame < 110; frame++)
            {
                await Frames(tree, 1);
                CanyonSkitterState state = skitter.CurrentFrame.State;
                sawRush |= state == CanyonSkitterState.Rush;
                sawRetreat |= state == CanyonSkitterState.Retreat;
                sawRecover |= state == CanyonSkitterState.Recover;
                minimum = MathF.Min(minimum, skitter.GlobalPosition.X);
                if (sawRush && state == CanyonSkitterState.Retreat)
                    retreatMaximum = MathF.Max(retreatMaximum, skitter.GlobalPosition.X);
            }
            TestAssert.True(sawRush && sawRetreat && sawRecover,
                "unobstructed left-facing actor completes warning, rush, retreat and recovery");
            TestAssert.True(minimum < 380f && minimum >= 335.5f,
                $"left rush moves the body over 20 px without exceeding 64 px; min_x={minimum}");
            TestAssert.True(retreatMaximum > minimum + 12f && retreatMaximum <= 400.5f,
                $"left retreat yields back toward home without overshooting; max_x={retreatMaximum}");
        }
        finally { root.QueueFree(); await Frames(tree, 2); }
    }

    private static async Task ThinWallAndSplitPerchStopBothDirections(SceneTree tree)
    {
        foreach (int side in new[] { -1, 1 })
        {
            var root = new Node2D { Name = $"SkitterThinWall{side}" };
            AddFloor(root, "Perch", new Vector2(300f, 220f), new Vector2(240f, 20f));
            CanyonSkitter skitter = NewSkitter(new Vector2(300f, 200f));
            root.AddChild(skitter);
            root.AddChild(LivingTarget(new Vector2(300f + side * 90f, 200f)));
            tree.Root.AddChild(root);
            try
            {
                for (int frame = 0; frame < 80 &&
                    skitter.CurrentFrame.State != CanyonSkitterState.Telegraph; frame++)
                    await Frames(tree, 1);
                TestAssert.True(skitter.CurrentFrame.State == CanyonSkitterState.Telegraph,
                    $"wall fixture begins a warning before adding the wall; side={side} " +
                    $"state={skitter.CurrentFrame.State} position={skitter.GlobalPosition} " +
                    $"floor={skitter.IsOnFloor()}");
                var wall = new StaticBody2D
                {
                    Name = "ThreePixelWall", Position = new Vector2(300f + side * 40f, 175f),
                    CollisionLayer = CombatCollisionLayers.World, CollisionMask = 0
                };
                wall.AddChild(new CollisionShape2D
                {
                    Shape = new RectangleShape2D { Size = new Vector2(3f, 70f) }
                });
                root.AddChild(wall);
                await Frames(tree, 90);
                TestAssert.True((skitter.GlobalPosition.X - 300f) * side < 27f,
                    "three-pixel wall stops the whole body, including reverse-facing rush");
            }
            finally { root.QueueFree(); await Frames(tree, 2); }
        }

        var split = new Node2D { Name = "SkitterSplitPerch" };
        AddFloor(split, "Perch240", new Vector2(300f, 220f), new Vector2(240f, 20f));
        AddFloor(split, "FarPerch", new Vector2(480f, 220f), new Vector2(80f, 20f));
        CanyonSkitter gapActor = NewSkitter(new Vector2(380f, 200f));
        split.AddChild(gapActor);
        split.AddChild(LivingTarget(new Vector2(470f, 200f)));
        tree.Root.AddChild(split);
        try
        {
            float maximum = gapActor.GlobalPosition.X;
            for (int i = 0; i < 100; i++)
            {
                await Frames(tree, 1);
                maximum = MathF.Max(maximum, gapActor.GlobalPosition.X);
            }
            TestAssert.True(maximum <= 408.5f,
                "complete 28-pixel footprint stops before an unsupported 20-pixel gap");
        }
        finally { split.QueueFree(); await Frames(tree, 2); }
    }

    private static async Task DeadHealthlessAndStackedTargetsCannotAggro(SceneTree tree)
    {
        var root = new Node2D { Name = "SkitterTargetGate" };
        AddFloor(root, "Top", new Vector2(300f, 220f), new Vector2(600f, 20f));
        AddFloor(root, "Lower", new Vector2(300f, 420f), new Vector2(600f, 20f));
        CanyonSkitter skitter = NewSkitter(new Vector2(300f, 200f));
        root.AddChild(skitter);
        Node2D dead = LivingTarget(new Vector2(390f, 200f));
        root.AddChild(dead);
        tree.Root.AddChild(root);
        await Frames(tree, 3);
        dead.GetNode<HealthComponent>("Health").ApplyDamage(
            new DamageRequest(99, Vector2.Right, 0f, 1001UL, 1001UL));
        try
        {
            await Frames(tree, 60);
            TestAssert.Equal(CanyonSkitterState.Idle, skitter.CurrentFrame.State,
                "dead player remaining in group is not a living target");
            dead.RemoveFromGroup("player");
            dead.QueueFree();
            var healthless = new Node2D { Name = "HealthlessPlayer", Position = new Vector2(390f, 200f) };
            healthless.AddToGroup("player");
            root.AddChild(healthless);
            await Frames(tree, 30);
            TestAssert.Equal(CanyonSkitterState.Idle, skitter.CurrentFrame.State,
                "healthless runtime double cannot create a threat");
            healthless.RemoveFromGroup("player");
            healthless.QueueFree();
            root.AddChild(LivingTarget(new Vector2(390f, 400f)));
            await Frames(tree, 30);
            TestAssert.Equal(CanyonSkitterState.Idle, skitter.CurrentFrame.State,
                "lower stacked shelf cannot trigger same-shelf aggro");
        }
        finally { root.QueueFree(); await Frames(tree, 2); }
    }

    private static async Task ThreadCancelsEachAttackPhaseImmediately(SceneTree tree)
    {
        foreach (CanyonSkitterState phase in new[] { CanyonSkitterState.Telegraph,
                     CanyonSkitterState.Rush, CanyonSkitterState.Retreat, CanyonSkitterState.Recover })
        {
            var root = new Node2D { Name = $"SkitterThread{phase}" };
            AddFloor(root, "Floor", new Vector2(300f, 220f), new Vector2(600f, 20f));
            CanyonSkitter skitter = NewSkitter(new Vector2(300f, 200f));
            root.AddChild(skitter);
            root.AddChild(LivingTarget(new Vector2(400f, 200f)));
            tree.Root.AddChild(root);
            try
            {
                await Until(tree, () => skitter.CurrentFrame.State == phase, 120);
                skitter.ApplyThreadPull(new Vector2(400f, -120f));
                TestAssert.Equal(CanyonSkitterState.Stunned, skitter.CurrentFrame.State,
                    $"thread immediately stuns from {phase}");
                TestAssert.True(!skitter.CurrentFrame.DamageActive &&
                    skitter.CurrentFrame.AttackTimeline.Phase == AttackPhase.None,
                    $"thread synchronously clears damage and attack identity from {phase}");
                await Frames(tree, 2);
                TestAssert.True(!skitter.GetNode<Hitbox>("RushHitbox").Monitoring,
                    "deferred physical monitor disable completes without signal-time error");
            }
            finally { root.QueueFree(); await Frames(tree, 2); }
        }
    }

    private static async Task RuntimeCrossBehindAndInvalidDeltasStaySafe(SceneTree tree)
    {
        foreach (bool duringRush in new[] { false, true })
        {
            var root = new Node2D { Name = duringRush ? "SkitterCrossRush" : "SkitterCrossLock" };
            AddFloor(root, "Floor", new Vector2(350f, 220f), new Vector2(600f, 20f));
            CharacterBody2D player = GD.Load<PackedScene>("res://scenes/player/Player.tscn")
                .Instantiate<CharacterBody2D>();
            player.GetNode<Camera2D>("Camera2D").Enabled = false;
            player.Position = new Vector2(410f, 174f);
            player.AddToGroup("player");
            CanyonSkitter skitter = NewSkitter(new Vector2(300f, 200f));
            root.AddChild(player);
            root.AddChild(skitter);
            tree.Root.AddChild(root);
            await Frames(tree, 3);
            player.GetNode<PlayerMotor>("Motor").SetPhysicsProcess(false);
            HealthComponent health = player.GetNode<HealthComponent>("Health");
            health.InvulnerabilitySeconds = 0f;
            try
            {
                if (duringRush)
                    await Until(tree, () => skitter.CurrentFrame.State == CanyonSkitterState.Rush, 100);
                else
                    await Until(tree, () => skitter.CurrentFrame.AttackTimeline.HasLockedTarget, 70);
                int before = health.CurrentHealth;
                player.GlobalPosition = new Vector2(skitter.GlobalPosition.X - 50f, 174f);
                await Frames(tree, 25);
                TestAssert.Equal(before, health.CurrentHealth,
                    "crossing behind a locked or active rush never creates a surprise rear hit");
                TestAssert.True(!skitter.CurrentFrame.DamageActive &&
                    !skitter.GetNode<Hitbox>("RushHitbox").Monitoring,
                    "cross-behind retires the old front attack identity");
            }
            finally { root.QueueFree(); await Frames(tree, 2); }
        }

        var invalidRoot = new Node2D { Name = "SkitterInvalidDelta" };
        AddFloor(invalidRoot, "Floor", new Vector2(300f, 220f), new Vector2(600f, 20f));
        CanyonSkitter invalidActor = NewSkitter(new Vector2(300f, 200f));
        invalidRoot.AddChild(invalidActor);
        tree.Root.AddChild(invalidRoot);
        await Frames(tree, 3);
        invalidActor.SetPhysicsProcess(false);
        try
        {
            foreach (double invalid in new[] { double.NaN, double.PositiveInfinity,
                         double.NegativeInfinity, double.MaxValue, -1d })
            {
                invalidActor.Velocity = Vector2.Zero;
                invalidActor._PhysicsProcess(invalid);
                TestAssert.True(float.IsFinite(invalidActor.Velocity.X) &&
                    float.IsFinite(invalidActor.Velocity.Y),
                    $"invalid actor delta {invalid} never poisons MoveAndSlide velocity");
                TestAssert.True(!invalidActor.CurrentFrame.DamageActive,
                    "invalid time alone cannot create an attack");
            }
        }
        finally { invalidRoot.QueueFree(); await Frames(tree, 2); }
    }

    private static async Task RealDirectionalShieldConsumesOneContact(SceneTree tree)
    {
        foreach (bool faceSource in new[] { true, false })
        {
            Input.ActionRelease(InputActions.Shield);
            var root = new Node2D { Name = faceSource ? "SkitterFrontGuard" : "SkitterRearGuard" };
            AddFloor(root, "Floor", new Vector2(360f, 220f), new Vector2(600f, 20f));
            CharacterBody2D player = GD.Load<PackedScene>("res://scenes/player/Player.tscn")
                .Instantiate<CharacterBody2D>();
            player.GetNode<Camera2D>("Camera2D").Enabled = false;
            player.Position = new Vector2(390f, 174f);
            player.AddToGroup("player");
            root.AddChild(player);
            CanyonSkitter skitter = NewSkitter(new Vector2(300f, 200f));
            root.AddChild(skitter);
            tree.Root.AddChild(root);
            await Frames(tree, 3);
            player.GetNode<PlayerMotor>("Motor").SetPhysicsProcess(false);
            HealthComponent health = player.GetNode<HealthComponent>("Health");
            health.InvulnerabilitySeconds = 0f;
            var input = player.GetNode<PlayerInputReader>("Input");
            var shield = player.GetNode<PlayerShieldController>("Shield");
            var energy = player.GetNode<PlayerEnergyController>("Energy");
            int guarded = 0;
            shield.ShieldResolved += (result, _) => { if (result.Guarded) guarded++; };
            Vector2 aimWorld = player.GlobalPosition + (faceSource ? Vector2.Left : Vector2.Right) * 100f;
            Vector2 pointer = player.GetViewport().GetFinalTransform() *
                (player.GetGlobalTransformWithCanvas() * player.ToLocal(aimWorld));
            var motion = new InputEventMouseMotion { Position = pointer, GlobalPosition = pointer };
            player.GetViewport().PushInput(motion, true);
            Input.ParseInputEvent(motion);
            Input.FlushBufferedEvents();
            Input.ActionPress(InputActions.Shield);
            input._PhysicsProcess(0d);
            shield._PhysicsProcess(0d);
            energy._PhysicsProcess(0d);
            shield._PhysicsProcess(0d);
            int before = health.CurrentHealth;
            try
            {
                await Frames(tree, 105);
                if (faceSource)
                {
                    TestAssert.True(guarded == 1 && health.CurrentHealth == before,
                        $"left-facing real shield blocks one front-side rush contact; guarded={guarded} " +
                        $"hp={health.CurrentHealth}/{before} state={skitter.CurrentFrame.State} " +
                        $"shield={shield.IsActive} direction={shield.GuardDirection} " +
                        $"actor={skitter.GlobalPosition} player={player.GlobalPosition}");
                }
                else
                    TestAssert.Equal(before - 1, health.CurrentHealth,
                        "right-facing shield takes exactly one wrong-side hit");
            }
            finally
            {
                Input.ActionRelease(InputActions.Shield);
                root.QueueFree();
                await Frames(tree, 2);
            }
        }
    }

    private static async Task PullFallLandingAndFallOutPreserveHealthSemantics(SceneTree tree)
    {
        var root = new Node2D { Name = "SkitterLowerLanding" };
        AddFloor(root, "Perch", new Vector2(300f, 220f), new Vector2(240f, 20f));
        AddFloor(root, "Lower", new Vector2(600f, 360f), new Vector2(600f, 20f));
        CanyonSkitter skitter = NewSkitter(new Vector2(395f, 200f));
        root.AddChild(skitter);
        tree.Root.AddChild(root);
        await Frames(tree, 3);
        HealthComponent health = skitter.GetNode<HealthComponent>("Health");
        health.ApplyDamage(new DamageRequest(1, Vector2.Right, 0f, 901UL, 901UL));
        skitter.ApplyThreadPull(new Vector2(600f, -80f));
        try
        {
            await Frames(tree, 100);
            TestAssert.Equal(1, health.CurrentHealth,
                "landing on an ordinary lower floor does not heal or kill a pulled enemy");
            TestAssert.True(skitter.GlobalPosition.Y > 250f,
                "pulled actor visibly leaves the original perch rather than teleporting home");
        }
        finally { root.QueueFree(); await Frames(tree, 2); }

        var fallRoot = new Node2D { Name = "SkitterFallOut" };
        AddFloor(fallRoot, "Perch", new Vector2(300f, 220f), new Vector2(240f, 20f));
        CanyonSkitter faller = NewSkitter(new Vector2(395f, 200f));
        fallRoot.AddChild(faller);
        tree.Root.AddChild(fallRoot);
        await Until(tree, () => faller.IsOnFloor(), 35);
        float authoredPerchY = faller.GlobalPosition.Y;
        int defeated = 0;
        faller.Defeated += () => defeated++;
        faller.ApplyThreadPull(new Vector2(600f, -80f));
        try
        {
            HealthComponent fallHealth = faller.GetNode<HealthComponent>("Health");
            for (int frame = 0; frame < 130 &&
                faller.GlobalPosition.Y < authoredPerchY + 240f; frame++)
            {
                await Frames(tree, 1);
                if (faller.GlobalPosition.Y < authoredPerchY + 240f)
                    TestAssert.True(fallHealth.CurrentHealth == 2 && defeated == 0,
                        "fall-out cannot kill before the 240-pixel drop threshold");
            }
            TestAssert.True(faller.GlobalPosition.Y >= authoredPerchY + 240f,
                "fixture actually crosses the 240-pixel drop threshold");
            TestAssert.True(fallHealth.CurrentHealth == 2 && defeated == 0,
                "crossing the drop distance alone cannot defeat the enemy");
            await Frames(tree, 10);
            TestAssert.True(fallHealth.CurrentHealth == 2 && defeated == 0,
                "unsupported falling for less than 0.25 seconds remains alive");
            await Until(tree, () => fallHealth.CurrentHealth == 0, 30);
            TestAssert.True(faller.GetNode<HealthComponent>("Health").CurrentHealth == 0,
                $"unsupported actor below perch plus 240 pixels receives environmental defeat; " +
                $"hp={faller.GetNode<HealthComponent>("Health").CurrentHealth} " +
                $"pos={faller.GlobalPosition} state={faller.CurrentFrame.State}");
            TestAssert.Equal(1, defeated, "fall-out emits Defeated once");
        }
        finally { fallRoot.QueueFree(); await Frames(tree, 2); }
    }

    private static async Task PullVelocityIsBoundedAndDoesNotAccumulateEveryFrame(SceneTree tree)
    {
        var root = new Node2D { Name = "SkitterBoundedPull" };
        AddFloor(root, "Floor", new Vector2(300f, 220f), new Vector2(600f, 20f));
        CanyonSkitter skitter = NewSkitter(new Vector2(300f, 200f));
        root.AddChild(skitter);
        tree.Root.AddChild(root);
        await Frames(tree, 25);
        float homeY = skitter.GlobalPosition.Y;
        float minimumY = homeY;
        skitter.ApplyThreadPull(new Vector2(0f, -540f));
        try
        {
            for (int i = 0; i < 125; i++)
            {
                await Frames(tree, 1);
                minimumY = MathF.Min(minimumY, skitter.GlobalPosition.Y);
            }
            TestAssert.True(minimumY >= homeY - 170f,
                $"one bounded 540-pixel-per-second pull cannot accumulate into runaway flight; " +
                $"rise={homeY - minimumY}");
            TestAssert.True(MathF.Abs(skitter.GlobalPosition.Y - homeY) < 3f,
                "a single upward claw pull lands back on the same floor within two seconds");
        }
        finally { root.QueueFree(); await Frames(tree, 2); }
    }

    // Catches timers advancing while the scene tree is paused or failing to resume afterward.
    private static async Task PausedWarningResumesWithoutSkippingRush(SceneTree tree)
    {
        var root = new Node2D { Name = "SkitterPauseResume" };
        AddFloor(root, "Floor", new Vector2(300f, 220f), new Vector2(600f, 20f));
        CanyonSkitter skitter = NewSkitter(new Vector2(300f, 200f));
        root.AddChild(skitter);
        root.AddChild(LivingTarget(new Vector2(400f, 200f)));
        tree.Root.AddChild(root);
        try
        {
            await Until(tree, () => skitter.CurrentFrame.State == CanyonSkitterState.Telegraph, 80);
            await Frames(tree, 8);
            ulong attackId = skitter.CurrentFrame.AttackTimeline.AttackId;
            Vector2 position = skitter.GlobalPosition;
            tree.Paused = true;
            await Frames(tree, 55);
            TestAssert.True(skitter.CurrentFrame.State == CanyonSkitterState.Telegraph &&
                skitter.CurrentFrame.AttackTimeline.AttackId == attackId &&
                skitter.GlobalPosition.DistanceTo(position) < .01f &&
                !skitter.GetNode<Hitbox>("RushHitbox").Monitoring,
                "pause freezes warning timer, actor position and damage without retiring identity");
            tree.Paused = false;
            await Until(tree, () => skitter.CurrentFrame.State == CanyonSkitterState.Rush, 55);
            TestAssert.Equal(attackId, skitter.CurrentFrame.AttackTimeline.AttackId,
                "resume finishes the same complete warning before a real rush");
        }
        finally
        {
            tree.Paused = false;
            root.QueueFree();
            await Frames(tree, 2);
        }
    }

    private static async Task LateSpawnResetAndRemovalClearAttack(SceneTree tree)
    {
        var root = new Node2D { Name = "SkitterLateSpawn" };
        AddFloor(root, "Floor", new Vector2(500f, 220f), new Vector2(600f, 20f));
        tree.Root.AddChild(root);
        CanyonSkitter skitter = NewSkitter(Vector2.Zero);
        root.AddChild(skitter);
        skitter.GlobalPosition = new Vector2(500f, 200f);
        root.AddChild(LivingTarget(new Vector2(600f, 200f)));
        try
        {
            await Until(tree, () => skitter.CurrentFrame.State == CanyonSkitterState.Rush, 100);
            skitter.GetNode<HealthComponent>("Health").ApplyDamage(
                new DamageRequest(1, Vector2.Left, 0f, 902UL, 902UL));
            skitter.ResetForCheckpoint();
            TestAssert.True(skitter.GlobalPosition.DistanceTo(new Vector2(500f, 200f)) < 2f,
                "checkpoint reset restores the actual nonzero post-AddChild spawn");
            TestAssert.Equal(2, skitter.GetNode<HealthComponent>("Health").CurrentHealth,
                "checkpoint reset restores exactly authored HP");
            TestAssert.True(!skitter.CurrentFrame.DamageActive &&
                skitter.CurrentFrame.AttackTimeline.Phase == AttackPhase.None,
                "reset clears rush and typed attack state");
            skitter.QueueFree();
            await Frames(tree, 2);
        }
        finally { root.QueueFree(); await Frames(tree, 2); }
    }

    private static async Task NativeVisualAndPresentationAreReplaceable(SceneTree tree)
    {
        var root = new Node2D { Name = "SkitterTypedPresentation" };
        AddFloor(root, "Floor", new Vector2(300f, 220f), new Vector2(600f, 20f));
        PrologueAudioDirector audio = PrologueAudioDirector.CreateRuntimeTree();
        audio.PlaybackEnabled = true;
        var vfx = new PrologueVfxDirector { Name = "Vfx", WorldEffectsPath = new NodePath("World") };
        vfx.AddChild(new Node2D { Name = "World" });
        CanyonSkitter skitter = NewSkitter(new Vector2(300f, 200f));
        root.AddChild(audio);
        root.AddChild(vfx);
        root.AddChild(skitter);
        root.AddChild(LivingTarget(new Vector2(400f, 200f)));
        tree.Root.AddChild(root);
        try
        {
            TestAssert.True(skitter.GetNodeOrNull<CanyonSkitterVisual>("VisualRoot/SkitterVisual") != null,
                "native six-legged skitter visual is attached under removable root");
            TestAssert.True(skitter.GetNodeOrNull<CanyonSkitterPresentation>("Presentation") != null,
                "local presentation forwards actor frames without room discovery");
            await Until(tree, () => skitter.CurrentFrame.State == CanyonSkitterState.Telegraph, 80);
            TestAssert.True(audio.FramePlaybackCount > 0 && vfx.TypedEffectCount == 1,
                "typed telegraph reaches both audio and VFX directly");
            TestAssert.Equal(PrologueAudioCue.RustShellTelegraph, audio.LastCue,
                "warning reuses the specified licensed provisional cue");
            int attackCount = audio.FramePlaybackCount;
            HealthComponent health = skitter.GetNode<HealthComponent>("Health");
            health.ApplyDamage(new DamageRequest(1, Vector2.Left, 0f, 1101UL, 1101UL));
            TestAssert.Equal(PrologueAudioCue.RustShellHurt, audio.LastCue,
                "actual damage forwards skitter hurt through the presentation node");
            TestAssert.Equal(attackCount, audio.FramePlaybackCount,
                "hurt cue does not duplicate attack-phase playback");
            health.ApplyDamage(new DamageRequest(1, Vector2.Left, 0f, 1102UL, 1102UL));
            TestAssert.Equal(PrologueAudioCue.RustShellDeath, audio.LastCue,
                "actual death forwards skitter death through the presentation node");
            TestAssert.Equal(0, vfx.TypedEffectCount,
                "death clears local typed VFX state");
            skitter.ResetForCheckpoint();
            foreach (Node visual in skitter.GetNode("VisualRoot").GetChildren()) visual.QueueFree();
            await Frames(tree, 2);
            float startX = skitter.GlobalPosition.X;
            await Frames(tree, 100);
            TestAssert.True(skitter.GlobalPosition.X != startX ||
                skitter.CurrentFrame.State != CanyonSkitterState.Idle,
                "combat still runs with every removable visual descendant absent");
        }
        finally { root.QueueFree(); await Frames(tree, 2); }
    }

    private static CanyonSkitter NewSkitter(Vector2 position)
    {
        CanyonSkitter skitter = GD.Load<PackedScene>("res://scenes/enemies/CanyonSkitter.tscn")
            .Instantiate<CanyonSkitter>();
        skitter.Position = position;
        return skitter;
    }

    private static Node2D LivingTarget(Vector2 position)
    {
        var player = new Node2D { Name = "LivingPlayer", Position = position };
        player.AddToGroup("player");
        player.AddChild(new HealthComponent { Name = "Health", MaxHealth = 5 });
        return player;
    }

    private static void AddFloor(Node2D root, string name, Vector2 position, Vector2 size)
    {
        var floor = new StaticBody2D
        {
            Name = name, Position = position,
            CollisionLayer = CombatCollisionLayers.World, CollisionMask = 0
        };
        floor.AddChild(new CollisionShape2D { Shape = new RectangleShape2D { Size = size } });
        root.AddChild(floor);
    }

    private static async Task Frames(SceneTree tree, int count)
    {
        for (int i = 0; i < count; i++) await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
    }

    private static async Task Until(SceneTree tree, Func<bool> condition, int maximum)
    {
        for (int i = 0; i < maximum && !condition(); i++) await Frames(tree, 1);
        TestAssert.True(condition(), "expected actor phase was reached within fixture deadline");
    }
}
