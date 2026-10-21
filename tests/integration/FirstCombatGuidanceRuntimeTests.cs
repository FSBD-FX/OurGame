using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Knight.Abilities;
using Knight.Combat;
using Knight.Enemies;
using Knight.Player;
using Knight.Prologue;
using Knight.Prologue.Guidance;
using Knight.Prologue.Seamless;
using Knight.Shared;
using Knight.Settings;
using Knight.Ui;

namespace Knight.Tests.Integration;

public static class FirstCombatGuidanceRuntimeTests
{
    internal static PrologueLevelController CreateLevelFixture()
    {
        PrologueLevelController level = GD.Load<PackedScene>("res://scenes/prologue/PrologueLevel.tscn")
            .Instantiate<PrologueLevelController>();
        CharacterBody2D player = level.GetNode<CharacterBody2D>("PlayerRoot/Player");
        // Controlled near-enemy setup, not a claim that the player walked this route.
        player.Position = new Vector2(500, 600);
        var world = level.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors");
        world.LayoutVersion = 2;
        world.InitialSector = PrologueSectorId.CanyonEntrance;
        PrologueGameFlowController flow = level.GetNode<PrologueGameFlowController>("GameFlow");
        flow.HeadlessAutoStart = false;
        flow.AutomationAutoStart = true;
        IsolatedPrologueFixture.Configure(level, "first-combat-guidance");
        return level;
    }

    public static async Task Run(SceneTree tree)
    {
        PrologueLevelController level = CreateLevelFixture();
        CharacterBody2D player = level.GetNode<CharacterBody2D>("PlayerRoot/Player");
        tree.Root.AddChild(level);
        ulong playerId = player.GetInstanceId();
        var playerHealth = player.GetNode<HealthComponent>("Health");
        int playerDamages = 0, playerDeaths = 0, heals = 0, requests = 0, recoveries = 0;
        void Damaged(int _, Vector2 __) => playerDamages++;
        void Died() => playerDeaths++;
        void Healed(int _, int __) => heals++;
        void Requested(string _) => requests++;
        void Recovered(string _) => recoveries++;
        playerHealth.Damaged += Damaged;
        playerHealth.Died += Died;
        playerHealth.Healed += Healed;
        level.RecoveryRequested += Requested;
        level.RecoveryCompleted += Recovered;
        try
        {
            var flow = level.GetNode<PrologueGameFlowController>("GameFlow");
            for (int frame = 0; frame < 60 && flow.State != PrologueGameFlowState.Playing; frame++)
                await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            TestAssert.Equal(PrologueGameFlowState.Playing, flow.State, "isolated current startup reaches Playing");
            var input = player.GetNode<PlayerInputReader>("Input");
            await KeyboardActionRuntimeTests.Frames(tree, input, 6);
            RustShellSquire enemy = await MountedEnemy(tree, level);
            ulong firstEnemyId = enemy.GetInstanceId();
            AssertSupported(level);
            TestAssert.True(enemy.IsVisibleInTree() && enemy.HasLineOfSightTo(player) &&
                Math.Abs(enemy.GlobalPosition.Y - player.GlobalPosition.Y) < 100f,
                "the real mounted first enemy is visible, same-height and has unobstructed sight");
            TestAssert.Equal(PrologueObjective.LearnAttackMode,
                level.GetNode<PrologueCompletionController>("Completion").CurrentObjective,
                "current attack objective is pending before real J");
            await AcceptedNativeJ(tree, level, enemy);
            AssertCombatGuidance(level, "controlled first encounter after real J");
            await CheckBoundaries(tree, level, player, enemy);
            TestAssert.True(requests == 0 && recoveries == 0, "all ordinary context cases need no recovery");
            await CheckRepeatedFailureCard(tree, level, player, firstEnemyId);
            TestAssert.True(requests == 2 && recoveries == 2 && playerDamages == 0 && playerDeaths == 0 && heals == 0 &&
                player.GetInstanceId() == playerId && level.Player.GetInstanceId() == playerId && playerHealth.CurrentHealth == 5,
                "same player retains HP5/no damage/death/healing and only the two explicitly controlled retry recoveries");
            GD.Print("FIRST_COMBAT_GUIDANCE_RUNTIME_OK layout=2 physical_keyboard=true controlled_arrangements=true route_claim=false");
        }
        finally
        {
            playerHealth.Damaged -= Damaged;
            playerHealth.Died -= Died;
            playerHealth.Healed -= Healed;
            level.RecoveryRequested -= Requested;
            level.RecoveryCompleted -= Recovered;
            foreach (Key key in new[] { Key.A, Key.D, Key.W, Key.S, Key.J, Key.K, Key.L, Key.Q, Key.Space })
                KeyboardActionRuntimeTests.KeyEvent(key, false);
            await TestSceneTeardown.FreeSceneAfterAudioDrain(tree, level);
        }
    }

    public static void AssertCombatGuidance(PrologueLevelController level, string stage,
        PrologueObjective expectedObjective = PrologueObjective.LearnGrappleMode)
    {
        PrologueGuidanceInstruction instruction = level.GetNode<PrologueGuidanceController>("Guidance").CurrentInstruction;
        Label ribbon = level.GetNode<Label>("Hud/FormalHud/ObjectiveRibbon/Text");
        PrologueObjective objective = level.GetNode<PrologueCompletionController>("Completion").CurrentObjective;
        GD.Print($"FIRST_COMBAT_GUIDANCE stage={stage} objective={objective} step={instruction.StepText} ribbon={ribbon.Text}");
        TestAssert.Equal(expectedObjective, objective,
            "contextual combat guidance does not consume or replace the underlying mechanical-grapple objective");
        TestAssert.True(instruction.Visible && HasCurrentCombatControls(instruction.StepText,
                level.GetNode<PrologueGuidanceController>("Guidance").Language),
            "first encounter explains actual shield and counterattack glyphs instead of the unrelated grapple lesson");
        TestAssert.True(instruction.StepText.Contains("1") &&
            !instruction.StepText.Contains("机械爪") && !instruction.StepText.Contains("grapple", StringComparison.OrdinalIgnoreCase),
            "combat guidance includes current weapon slot without obsolete Q/E mode switching");
        string title = ribbon.Text;
        TestAssert.True(ribbon.IsVisibleInTree() &&
            (title.Contains("敌人") || title.Contains("enemy", StringComparison.OrdinalIgnoreCase) ||
             title.Contains("战斗") || title.Contains("防御") || title.Contains("反击") ||
             title.Contains("combat", StringComparison.OrdinalIgnoreCase) || title.Contains("counter", StringComparison.OrdinalIgnoreCase)) &&
            !title.Contains("机械爪") && !title.Contains("grapple", StringComparison.OrdinalIgnoreCase),
            "the actual objective ribbon presents a combat title while the underlying objective remains unchanged");
    }

    private static async Task CheckBoundaries(SceneTree tree, PrologueLevelController level,
        CharacterBody2D player, RustShellSquire enemy)
    {
        var guidance = level.GetNode<PrologueGuidanceController>("Guidance");
        var formal = level.GetNode<PrologueFormalHud>("Hud/FormalHud");
        guidance.SetLanguage(GameLanguage.English);
        formal.SetLanguage(GameLanguage.English);
        string ordinary = formal.CurrentModel.Objective;
        float clock = formal.CurrentModel.ObjectiveTransitionSeconds;
        guidance.SetGuidanceEnabled(false);
        await Settle(tree);
        TestAssert.True(!guidance.CurrentInstruction.Visible, "disabled guidance hides the combat card");
        AssertOrdinary(level, ordinary, "guidance disabled");
        guidance.SetGuidanceEnabled(true);
        await Settle(tree);
        AssertCombatGuidance(level, "guidance re-enabled");
        TestAssert.Equal(ordinary, formal.CurrentModel.Objective, "temporary combat title never replaces HUD model objective");
        TestAssert.True(formal.CurrentModel.ObjectiveTransitionSeconds >= clock,
            "combat title toggles do not restart the ordinary objective transition clock");

        // Explicit world arrangements exercise runtime predicates, not player route completion.
        foreach (var sample in new[] { (420f, false), (360f, false), (330f, true), (390f, true), (420f, false) })
        {
            await PlaceEnemy(tree, player, enemy, sample.Item1);
            TestAssert.True(Math.Abs(enemy.GlobalPosition.X - player.GlobalPosition.X - sample.Item1) < 12f,
                "scheduled actor motion stays within the arranged hysteresis test margin");
            if (sample.Item2) AssertCombatGuidance(level, $"hysteresis distance {sample.Item1}");
            else AssertOrdinary(level, ordinary, $"hysteresis distance {sample.Item1}");
        }
        await PlaceEnemy(tree, player, enemy, 210f);
        enemy.Visible = false;
        await Settle(tree);
        AssertOrdinary(level, ordinary, "hidden enemy");
        enemy.Visible = true;
        await Settle(tree);
        AssertCombatGuidance(level, "visible enemy restored");
        await PlaceEnemy(tree, player, enemy, 210f, -140f);
        AssertOrdinary(level, ordinary, "enemy on a different height layer");
        await PlaceEnemy(tree, player, enemy, 210f);
        AssertCombatGuidance(level, "same floor restored");

        var wall = new StaticBody2D { Name = "GuidanceSightTestWall", CollisionLayer = CombatCollisionLayers.World };
        wall.AddChild(new CollisionShape2D { Shape = new RectangleShape2D { Size = new Vector2(20, 180) } });
        level.AddChild(wall);
        wall.GlobalPosition = player.GlobalPosition + new Vector2(100, -30);
        try
        {
            await Settle(tree);
            TestAssert.True(!enemy.HasLineOfSightTo(player), "the temporary world wall really blocks enemy sight");
            AssertOrdinary(level, ordinary, "world wall blocks encounter");
        }
        finally { wall.QueueFree(); await Settle(tree); }
        AssertCombatGuidance(level, "wall removed");

        await RealTerrainAttachment(tree, level);
        TestAssert.Equal(PrologueObjective.ReachBasin,
            level.GetNode<PrologueCompletionController>("Completion").CurrentObjective,
            "real exact-target K attachment advances the ordinary objective independently of contextual teaching");
        ordinary = "REACH THE CANYON BASIN";
        TestAssert.True(enemy.IsVisibleInTree() && enemy.GetNode<HealthComponent>("Health").CurrentHealth > 0 &&
            enemy.HasLineOfSightTo(player) && Math.Abs(enemy.GlobalPosition.X - player.GlobalPosition.X) <= 340 &&
            Math.Abs(enemy.GlobalPosition.Y - player.GlobalPosition.Y) <= 100 &&
            player.GetNode<MechanicalClawController>("MechanicalClaw").CurrentFrame.State == MechanicalClawState.Idle &&
            player.GetNode<PlayerActionModeController>("ActionModes").CurrentMode == PlayerActionMode.Grapple,
            "cancelled real K keeps Grapple metadata while a visible living same-layer nearby enemy remains eligible otherwise");
        AssertOrdinary(level, ordinary, "actual terrain claw completes teaching and ordinary exit guidance resumes");
        var routeMarker = level.GetNode<WorldTargetMarkerVisual>("Hud/Guidance/VisualRoot");
        TestAssert.True(routeMarker.RenderedTargetCount > 0,
            "completed real claw at the entrance keeps a rendered forward route target instead of descent-only ice");
        Node2D nextSeam = level.CurrentSectorRoot.GetNode<Node2D>("Gameplay/RouteMarkers/NextSeam");
        bool pointsToExit = false;
        foreach (PrologueGuidanceTargetKey key in guidance.CurrentInstruction.Targets)
            pointsToExit |= PrologueGuidanceTargetResolver.TryResolve(level.CurrentSectorRoot,
                level.CurrentSector.Value, key, out Node2D target) && target == nextSeam;
        TestAssert.True(pointsToExit && nextSeam.GlobalPosition.X > player.GlobalPosition.X,
            "entrance route teaching points to the real forward seam, not back to the attack lesson");
        enemy.Visible = true;
        await PlaceEnemy(tree, player, enemy, 210f);
        await Pulse(tree, Key.J);
        AssertCombatGuidance(level, "real J restores context over the latest objective", PrologueObjective.ReachBasin);
        enemy.Visible = false;
        await Settle(tree);
        AssertOrdinary(level, ordinary, "context exits to latest ordinary objective, not cached grapple objective");
        enemy.Visible = true;
        await CheckLayoutVariants(tree, level, player, enemy);

        HealthComponent health = enemy.GetNode<HealthComponent>(enemy.HealthPath);
        TestAssert.True(health.ApplyDamage(new DamageRequest(health.CurrentHealth, Vector2.Zero, 0f, 991UL)),
            "controlled lethal damage is accepted by the real first enemy health component");
        await Settle(tree);
        AssertOrdinary(level, ordinary, "dead enemy ends contextual teaching");
    }

    private static async Task CheckRepeatedFailureCard(SceneTree tree, PrologueLevelController level,
        CharacterBody2D player, ulong originalEnemyId)
    {
        var guidance = level.GetNode<PrologueGuidanceController>("Guidance");
        var visual = level.GetNode<WorldTargetMarkerVisual>("Hud/Guidance/VisualRoot");
        var sector = level.CurrentSector;
        int recoveries = 0;
        void OnRecovery(string reason) { if (reason == "DEATH RESET") recoveries++; }
        level.RecoveryCompleted += OnRecovery;
        try
        {
            for (int i = 0; i < 2; i++)
            {
                await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
                level.RequestRespawn("DEATH RESET");
                await Settle(tree);
                TestAssert.Equal(i + 1, recoveries, "each real same-sector recovery completes before requesting another");
                TestAssert.Equal(sector, level.CurrentSector, "failure-card fixture remains in the original sector");
            }
        }
        finally { level.RecoveryCompleted -= OnRecovery; }

        // This is a controlled context arrangement, NOT an input-route traversal.
        // Recovery retires actors at start160,600, outside the encounter activation.
        player.GlobalPosition = new Vector2(500, 600);
        var enemy = await MountedEnemy(tree, level);
        TestAssert.True(enemy.GetInstanceId() != originalEnemyId, "recovery reactivation mounts a genuinely new Rust instance");
        AssertSupported(level);
        await PlaceEnemy(tree, player, enemy, 210f);
        await Pulse(tree, Key.J);
        TestAssert.True(guidance.SameSectorFailureCount >= 2 &&
            !string.IsNullOrEmpty(guidance.CurrentInstruction.ContextTitle),
            "two completed same-sector recoveries really restore the nearby combat context");
        GD.Print($"FIRST_COMBAT_RETRY_CARD failures={guidance.SameSectorFailureCount} " +
            $"context={guidance.CurrentInstruction.ContextTitle} expanded={visual.IsExpandedTeaching}");
        TestAssert.True(!visual.IsExpandedTeaching,
            "nearby combat teaching stays compact after two real same-sector failures");

        enemy.Visible = false;
        await Settle(tree);
        PrologueGuidanceInstruction ordinary = guidance.CurrentInstruction;
        TestAssert.True(string.IsNullOrEmpty(ordinary.ContextTitle) && visual.IsExpandedTeaching &&
            !string.IsNullOrEmpty(ordinary.HintText) && visual.GetNode<Label>("Text").Text.Contains(ordinary.HintText),
            "leaving combat restores the ordinary expanded retry hint in the actual card");
    }

    private static async Task CheckLayoutVariants(SceneTree tree, PrologueLevelController level,
        CharacterBody2D player, RustShellSquire enemy)
    {
        Vector2I oldSize = tree.Root.Size, oldContentSize = tree.Root.ContentScaleSize;
        var formal = level.GetNode<PrologueFormalHud>("Hud/FormalHud");
        var guidance = level.GetNode<PrologueGuidanceController>("Guidance");
        try
        {
            foreach (Vector2I size in new[] { new Vector2I(1280, 720), new Vector2I(1920, 1080) })
            foreach (GameLanguage language in new[] { GameLanguage.ChineseSimplified, GameLanguage.English })
            foreach (float scale in new[] { .8f, 1.5f })
            {
                tree.Root.Size = size;
                tree.Root.ContentScaleSize = size;
                formal.SetUiScale(scale);
                formal.SetLanguage(language);
                guidance.SetLanguage(language);
                await PlaceEnemy(tree, player, enemy, 210f);
                string context = $"{size} {language} scale={scale}";
                AssertCombatGuidance(level, context, PrologueObjective.ReachBasin);
                var marker = level.GetNode<WorldTargetMarkerVisual>("Hud/Guidance/VisualRoot");
                Label text = marker.GetNode<Label>("Text");
                Label title = formal.GetNode<Label>("ObjectiveRibbon/Text");
                Rect2 ribbon = formal.GetNode<Control>("ObjectiveRibbon").GetGlobalRect();
                TestAssert.True(marker.HasVisiblePrompt && text.IsVisibleInTree() && HasCurrentCombatControls(text.Text, language),
                    "actual combat card retains both actionable glyphs at " + context);
                TestAssert.True(marker.PromptRect.Grow(.5f).Encloses(text.GetRect()) &&
                    text.GetMinimumSize().Y <= text.Size.Y + .5f,
                    "actual shaped guidance label fits its card without losing its last line at " + context);
                TestAssert.True(ribbon.Grow(.5f).Encloses(title.GetGlobalRect()) &&
                    title.GetMinimumSize().Y <= title.Size.Y + .5f &&
                    title.GetThemeFont("font").GetStringSize(title.Text, HorizontalAlignment.Left, -1,
                        title.GetThemeFontSize("font_size")).X <= title.Size.X + .5f,
                    "localized contextual ribbon title fits without ellipsis at " + context);
                TestAssert.True(!ribbon.Intersects(marker.PromptRect) &&
                    new Rect2(Vector2.Zero, level.GetViewportRect().Size).Encloses(marker.PromptRect),
                    "combat card stays on-screen and separate from contextual objective at " + context);
                guidance.SetGuidanceEnabled(false);
                await Settle(tree);
                TestAssert.Equal(language == GameLanguage.ChineseSimplified ? "抵达峡谷盆地" : "REACH THE CANYON BASIN",
                    title.Text, "disabling context restores the actual localized ordinary title at " + context);
                TestAssert.Equal("REACH THE CANYON BASIN", formal.CurrentModel.Objective,
                    "localized restoration leaves the ordinary HUD model unchanged at " + context);
                guidance.SetGuidanceEnabled(true);
                await Settle(tree);
                AssertCombatGuidance(level, "localized context restored " + context, PrologueObjective.ReachBasin);
            }
        }
        finally
        {
            tree.Root.Size = oldSize;
            tree.Root.ContentScaleSize = oldContentSize;
            formal.SetUiScale(1f);
            formal.SetLanguage(GameLanguage.English);
            guidance.SetLanguage(GameLanguage.English);
            await Settle(tree);
        }
    }

    private static void AssertOrdinary(PrologueLevelController level, string objective, string stage)
    {
        var formal = level.GetNode<PrologueFormalHud>("Hud/FormalHud");
        TestAssert.Equal(objective, formal.CurrentModel.Objective, "ordinary HUD model preserved: " + stage);
        TestAssert.Equal(objective, formal.GetNode<Label>("ObjectiveRibbon/Text").Text,
            "actual ribbon restores the current ordinary English title: " + stage);
        TestAssert.True(string.IsNullOrEmpty(level.GetNode<PrologueGuidanceController>("Guidance").CurrentInstruction.ContextTitle),
            "combat card is withdrawn: " + stage);
    }

    private static async Task PlaceEnemy(SceneTree tree, CharacterBody2D player, RustShellSquire enemy,
        float distance, float height = 0f)
    {
        enemy.GlobalPosition = player.GlobalPosition + new Vector2(distance, height);
        await Settle(tree);
    }

    private static async Task Settle(SceneTree tree)
    {
        for (int i = 0; i < 2; i++) await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
        for (int i = 0; i < 3; i++) await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
    }

    private static async Task Pulse(SceneTree tree, Key key)
    {
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        KeyboardActionRuntimeTests.KeyEvent(key, true);
        await Settle(tree);
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        KeyboardActionRuntimeTests.KeyEvent(key, false);
        await Settle(tree);
    }

    private static async Task<RustShellSquire> MountedEnemy(SceneTree tree, PrologueLevelController level)
    {
        CanyonEncounterGroup group = level.CurrentSectorRoot.FindChildren("*", "", true, false)
            .OfType<CanyonEncounterGroup>().Single(node => node.EncounterId == "canyon.v2.encounter.low-ceiling-bridge");
        for (int tick = 0; tick < 60 && (!group.IsActive || group.LiveActorCount != 1); tick++)
            await KeyboardActionRuntimeTests.Frames(tree, level.Player.GetNode<PlayerInputReader>("Input"), 1);
        TestAssert.True(group.IsActive && group.CurrentBatch == 0 && group.LiveActorCount == 1,
            "real original encounter activates one first-batch actor without direct spawning");
        RustShellSquire enemy = group.GetNode(group.ActorRootPath).GetChildren().OfType<RustShellSquire>()
            .Single(node => GodotObject.IsInstanceValid(node) && node.IsInsideTree() && !node.IsQueuedForDeletion());
        TestAssert.True(enemy.SceneFilePath == "res://scenes/enemies/RustShellSquire.tscn" &&
            enemy.GetNode<HealthComponent>("Health").CurrentHealth == 3,
            "unique genuine saved Rust actor has serialized three HP, not the guidance-only Marker");
        return enemy;
    }

    private static void AssertSupported(PrologueLevelController level)
    {
        CharacterBody2D player = level.Player;
        var floor = level.CurrentSectorRoot.GetNode<StaticBody2D>("Gameplay/Collision/BridgeFarBank");
        var floorCollider = floor.GetNode<CollisionShape2D>("Collision");
        var rectangle = (RectangleShape2D)floorCollider.Shape;
        var collider = player.GetNode<CollisionShape2D>("BodyCollider");
        var capsule = (CapsuleShape2D)collider.Shape;
        Rect2 bounds = new(floorCollider.GlobalPosition - rectangle.Size * .5f, rectangle.Size);
        bool contact = false;
        for (int index = 0; player.IsOnFloor() && index < player.GetSlideCollisionCount(); index++)
        {
            var hit = player.GetSlideCollision(index);
            contact |= hit.GetCollider() == floor && hit.GetNormal().Y < -.7f;
        }
        TestAssert.True(level.CurrentSector == PrologueSectorId.CanyonEntrance && contact && !floorCollider.Disabled &&
            bounds.Position.IsEqualApprox(new Vector2(400, 636)) && bounds.Size.IsEqualApprox(new Vector2(880, 84)) &&
            Mathf.IsZeroApprox(floorCollider.GlobalRotation) && floorCollider.GlobalScale.IsEqualApprox(Vector2.One) &&
            collider.Position.IsZeroApprox() && Mathf.IsZeroApprox(collider.GlobalRotation) && collider.GlobalScale.IsEqualApprox(Vector2.One) &&
            Mathf.IsEqualApprox(capsule.Height, 72) && Mathf.IsEqualApprox(capsule.Radius, 20) &&
            player.GlobalPosition.X - 20 > bounds.Position.X && player.GlobalPosition.X + 20 < bounds.End.X &&
            Mathf.Abs(player.GlobalPosition.Y + 36 - 636) < .5f &&
            player.GetNode<HealthComponent>("Health").CurrentHealth == 5,
            "same default-health player has complete actual72x40 capsule support on current saved FarBank");
    }

    private static async Task AcceptedNativeJ(SceneTree tree, PrologueLevelController level, RustShellSquire enemy)
    {
        CharacterBody2D player = level.Player;
        var combat = player.GetNode<PlayerCombat>("Combat");
        var input = player.GetNode<PlayerInputReader>("Input");
        var health = enemy.GetNode<HealthComponent>("Health");
        var hurtbox = enemy.GetNode<Hurtbox>("Hurtbox");
        int starts = 0, connections = 0, damages = 0;
        ulong attackId = 0, damageAttack = 0;
        ulong playerId = player.GetInstanceId(), enemyId = enemy.GetInstanceId();
        void Started(ulong id, int _) { starts++; attackId = id; }
        void Connected(Hurtbox box) { if (box == hurtbox && ReferenceEquals(box.ResolveDamageReceiver(), health)) connections++; }
        void Damaged(int remaining, Vector2 _) { if (remaining == 2) { damages++; damageAttack = attackId; } }
        combat.AttackStarted += Started;
        combat.AttackConnected += Connected;
        health.Damaged += Damaged;
        try
        {
            KeyboardActionRuntimeTests.KeyEvent(Key.J, true);
            await KeyboardActionRuntimeTests.Frames(tree, input, 3);
            KeyboardActionRuntimeTests.KeyEvent(Key.J, false);
            for (int tick = 0; tick < 20 && health.CurrentHealth == 3; tick++)
                await KeyboardActionRuntimeTests.Frames(tree, input, 1);
            TestAssert.True(starts == 1 && attackId != 0 && damageAttack == attackId &&
                connections == 1 && damages == 1 && health.CurrentHealth == 2 &&
                player.GetInstanceId() == playerId && enemy.GetInstanceId() == enemyId,
                "physical J causes one exact native enemy hit/Health3→2 for its accepted nonzero AttackId");
            // Controlled UI predicate fixture, not natural combat-victory proof:
            // move the already-hit actor away before observing layout/visibility cases.
            await PlaceEnemy(tree, player, enemy, 210f);
            await KeyboardActionRuntimeTests.Frames(tree, input, 30);
            AssertSupported(level);
            TestAssert.Equal(PrologueObjective.LearnGrappleMode,
                level.GetNode<PrologueCompletionController>("Completion").CurrentObjective,
                "only actual accepted J naturally advances current attack teaching");
            GD.Print($"FIRST_COMBAT_NATIVE_J_OK attack={attackId} player={playerId} enemy={enemyId} hp=3→2 controlled_pose=true");
        }
        finally
        {
            KeyboardActionRuntimeTests.KeyEvent(Key.J, false);
            combat.AttackStarted -= Started;
            combat.AttackConnected -= Connected;
            health.Damaged -= Damaged;
        }
    }

    private static async Task RealTerrainAttachment(SceneTree tree, PrologueLevelController level)
    {
        CharacterBody2D player = level.Player;
        var input = player.GetNode<PlayerInputReader>("Input");
        var claw = player.GetNode<MechanicalClawController>("MechanicalClaw");
        var target = level.CurrentSectorRoot.GetNode<GrappleSurface2D>("Gameplay/Collision/LowBridgeCeiling/GrappleSurfaceProfile");
        KeyboardActionRuntimeTests.KeyEvent(Key.W, true);
        KeyboardActionRuntimeTests.KeyEvent(Key.A, true);
        try
        {
            await KeyboardActionRuntimeTests.Frames(tree, input, 2);
            var preview = claw.KeyboardPreview;
            TestAssert.True(preview.Surface == target && preview.Snapshot.Exists && preview.Snapshot.HasLineOfSight &&
                preview.Snapshot.SurfaceNormal.IsEqualApprox(Vector2.Right) &&
                Mathf.Abs(preview.Snapshot.WorldAnchor.X - 400) < .5f &&
                preview.Snapshot.WorldAnchor.Y is >= 484f and <= 508f,
                "real W+A preview chooses actual LowBridge right face, not a dock over the gap");
            var capsule = (CapsuleShape2D)player.GetNode<CollisionShape2D>("BodyCollider").Shape;
            var settings = MechanicalClawSettings.Default;
            Vector2 dock = MechanicalClawArrivalGeometry.DockPoint(preview.Snapshot.WorldAnchor, Vector2.Right,
                capsule, settings.MinimumRopeLength, settings.ArrivalMargin);
            TestAssert.True(Mathf.Abs(dock.X - 448) < .5f &&
                !player.TestMove(player.GlobalTransform, dock - player.GlobalPosition, null, player.SafeMargin, false),
                "native entire capsule sweep to right-face dock448 is clear of actual ceiling and bank edge");
            ulong previous = claw.CurrentFrame.NextCastId;
            KeyboardActionRuntimeTests.KeyEvent(Key.K, true);
            for (int tick = 0; tick < 12 && claw.CurrentFrame.LockedTargetRuntimeId != target.GetInstanceId(); tick++)
                await KeyboardActionRuntimeTests.Frames(tree, input, 1);
            TestAssert.True(claw.CurrentFrame.NextCastId == previous + 1 &&
                claw.CurrentFrame.LockedTargetRuntimeId == target.GetInstanceId(),
                "one real fresh K locks exact terrain target before aim keys release");
            KeyboardActionRuntimeTests.KeyEvent(Key.A, false);
            KeyboardActionRuntimeTests.KeyEvent(Key.W, false);
            KeyboardActionRuntimeTests.KeyEvent(Key.K, false);
            for (int tick = 0; tick < 40 && !Attached(claw.CurrentFrame.State); tick++)
                await KeyboardActionRuntimeTests.Frames(tree, input, 1);
            TestAssert.True(Attached(claw.CurrentFrame.State) && claw.CurrentTarget == target &&
                claw.CurrentFrame.SurfaceNormal.IsEqualApprox(Vector2.Right) && Mathf.Abs(claw.CurrentFrame.DockPoint.X - 448) < .5f,
                "actual live K attachment keeps exact right-face lock and dock");
            await Settle(tree);
            var instruction = level.GetNode<PrologueGuidanceController>("Guidance").CurrentInstruction;
            var visual = level.GetNode<WorldTargetMarkerVisual>("Hud/Guidance/VisualRoot");
            TestAssert.True(instruction.Visible && string.IsNullOrEmpty(instruction.ContextTitle) &&
                instruction.StepText.Contains("auto-pull", StringComparison.OrdinalIgnoreCase) &&
                instruction.StepText.Contains("AFTER ARRIVAL") && instruction.StepText.Contains("SPACE") &&
                instruction.StepText.Contains("K AGAIN: CANCEL") && visual.HasVisiblePrompt &&
                visual.RenderedStepText == instruction.StepText &&
                level.GetNode<PrologueCompletionController>("Completion").CurrentObjective == PrologueObjective.ReachBasin,
                "actual attached claw guidance outranks nearby combat, without injected objective");
            KeyboardActionRuntimeTests.KeyEvent(Key.K, true);
            await KeyboardActionRuntimeTests.Frames(tree, input, 3);
            KeyboardActionRuntimeTests.KeyEvent(Key.K, false);
            int stable = 0;
            for (int tick = 0; tick < 180 && stable < 6; tick++)
            {
                await KeyboardActionRuntimeTests.Frames(tree, input, 1);
                stable = player.IsOnFloor() && claw.CurrentFrame.State == MechanicalClawState.Idle ? stable + 1 : 0;
            }
            TestAssert.Equal(6, stable, "K cancel naturally returns six stable supported Idle samples");
            AssertSupported(level);
        }
        finally
        {
            foreach (Key key in new[] { Key.A, Key.W, Key.K }) KeyboardActionRuntimeTests.KeyEvent(key, false);
        }
    }

    private static bool Attached(MechanicalClawState state) =>
        state is MechanicalClawState.Latched or MechanicalClawState.Reel or MechanicalClawState.Swing;

    private static bool HasCurrentCombatControls(string text, GameLanguage language) =>
        text.Contains("WASD") && !text.Contains("RMB") && !text.Contains("LMB") &&
        (language == GameLanguage.English
            ? text.Contains("1: WEAPON") && text.Contains("J: ATTACK") && text.Contains("HOLD L") &&
                text.Contains("RELEASE L") && text.Contains("PRESS J AGAIN")
            : text.Contains("1 选武器") && text.Contains("J 攻击") && text.Contains("按住 L") &&
                text.Contains("松 L") && text.Contains("再按 J"));
}
