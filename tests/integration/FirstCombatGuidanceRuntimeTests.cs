using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Knight.Abilities;
using Knight.Combat;
using Knight.Enemies;
using Knight.Player;
using Knight.Prologue;
using Knight.Prologue.Guidance;
using Knight.Prologue.Residents;
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
        await CheckRecoveryWaitRejections(tree);
        PrologueLevelController level = CreateLevelFixture();
        CharacterBody2D player = level.GetNode<CharacterBody2D>("PlayerRoot/Player");
        tree.Root.AddChild(level);
        ulong playerId = player.GetInstanceId();
        var playerHealth = player.GetNode<HealthComponent>("Health");
        int playerDamages = 0, playerDeaths = 0, heals = 0, requests = 0, recoveries = 0;
        var observedInput = player.GetNode<PlayerInputReader>("Input");
        string observationStage = "startup";
        var evidence = new List<CombatHealthEvidence>(128);
        int evidenceOverflow = 0;
        void Observe(string kind, int signalRemaining = -1, Vector2 impulse = default, int restored = 0)
        {
            // Bounded scalars only: no physics query, tree traversal, frame wait,
            // input, state mutation or printing inside signal callbacks.
            if (evidence.Count == 128) { evidenceOverflow++; return; }
            evidence.Add(new(observationStage, kind, Engine.GetPhysicsFrames(), Engine.GetProcessFrames(),
                observedInput.CurrentFrameSequence, playerHealth.CurrentHealth, signalRemaining, restored, impulse.X, impulse.Y,
                player.GlobalPosition.X, player.GlobalPosition.Y, playerDamages, playerDeaths, heals, requests, recoveries));
        }
        void Stage(string stage) { observationStage = stage; Observe("stage"); }
        void Damaged(int remaining, Vector2 impulse) { playerDamages++; Observe("damaged", remaining, impulse); }
        void Died() { playerDeaths++; Observe("died"); }
        void Healed(int amount, int current) { heals++; Observe("healed", current, restored: amount); }
        void Requested(string reason) { requests++; Observe("recovery-request:" + reason); }
        void Recovered(string reason) { recoveries++; Observe("recovery-complete:" + reason); }
        playerHealth.Damaged += Damaged;
        playerHealth.Died += Died;
        playerHealth.Healed += Healed;
        level.RecoveryRequested += Requested;
        level.RecoveryCompleted += Recovered;
        try
        {
            var flow = level.GetNode<PrologueGameFlowController>("GameFlow");
            ulong startupPhysics = Engine.GetPhysicsFrames(), startupMs = Time.GetTicksMsec();
            for (int frame = 0; frame < 60 && flow.State != PrologueGameFlowState.Playing; frame++)
                await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            if (flow.State != PrologueGameFlowState.Playing)
                GD.Print($"FIRST_COMBAT_STARTUP_FAILURE state={flow.State} pending={level.ResidentPreparationPending} " +
                    $"reason={System.Text.Json.JsonSerializer.Serialize(level.ResidentPreparationFailure)} " +
                    $"rejected={System.Text.Json.JsonSerializer.Serialize(flow.LastRejectedTransition)} " +
                    $"load_status={flow.LoadResult?.Status.ToString() ?? "null"} needs={level.NeedsResidentPreparation} " +
                    $"layout={level.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors").LayoutVersion} content={level.ResidentContent} " +
                    $"player={player.GlobalPosition} velocity={player.Velocity} can_process={player.CanProcess()} " +
                    $"observed_physics={Engine.GetPhysicsFrames() - startupPhysics} elapsed_ms={Time.GetTicksMsec() - startupMs}");
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
            if (!HasCurrentCombatControls(level.GetNode<PrologueGuidanceController>("Guidance").CurrentInstruction.StepText,
                    level.GetNode<PrologueGuidanceController>("Guidance").Language))
            {
                foreach (RustShellSquire candidate in level.CurrentSectorRoot.GetNode("Gameplay/Actors")
                    .GetChildren().OfType<RustShellSquire>())
                    GD.Print("FIRST_COMBAT_CONTEXT_CANDIDATE " + System.Text.Json.JsonSerializer.Serialize(new
                    {
                        Id = candidate.GetInstanceId(), ExactOriginal = candidate == enemy,
                        Path = candidate.GetPath().ToString(), Scene = candidate.SceneFilePath,
                        Resident = candidate.HasNode("ResidentBinding"), Position = candidate.GlobalPosition.ToString(),
                        Separation = (candidate.GlobalPosition - player.GlobalPosition).ToString(),
                        Visible = candidate.IsVisibleInTree(), Health = candidate.GetNode<HealthComponent>("Health").CurrentHealth,
                        InViewport = level.GetViewportRect().HasPoint(candidate.GetGlobalTransformWithCanvas().Origin),
                        Sight = candidate.HasLineOfSightTo(player), Mode = player.GetNode<PlayerActionModeController>("ActionModes").CurrentMode.ToString(),
                        Claw = player.GetNode<MechanicalClawController>("MechanicalClaw").CurrentFrame.State.ToString()
                    }));
                ObserveFailedOriginalSight(level, enemy);
            }
            AssertCombatGuidance(level, "controlled first encounter after real J");
            Stage("boundaries");
            await CheckBoundaries(tree, level, player, enemy, Stage);
            TestAssert.True(requests == 0 && recoveries == 0, "all ordinary context cases need no recovery");
            Stage("retry-entry");
            await CheckRepeatedFailureCard(tree, level, player, firstEnemyId, Stage);
            Stage("final-contract");
            TestAssert.True(requests == 2 && recoveries == 2 && playerDamages == 0 && playerDeaths == 0 && heals == 0 &&
                player.GetInstanceId() == playerId && level.Player.GetInstanceId() == playerId && playerHealth.CurrentHealth == 5,
                "same player retains HP5/no damage/death/healing and only the two explicitly controlled retry recoveries");
            GD.Print("FIRST_COMBAT_GUIDANCE_RUNTIME_OK layout=2 physical_keyboard=true controlled_arrangements=true route_claim=false");
        }
        catch
        {
            // Failure-only serialization cannot replace the original exception.
            try
            {
                GD.Print("FIRST_COMBAT_HEALTH_FAILURE " + System.Text.Json.JsonSerializer.Serialize(new
                {
                    Stage = observationStage, requests, recoveries, playerDamages, playerDeaths, heals,
                    PlayerId = playerId, CurrentPlayerId = player.GetInstanceId(),
                    LevelPlayerId = level.Player.GetInstanceId(), Hp = playerHealth.CurrentHealth,
                    Physics = Engine.GetPhysicsFrames(), Process = Engine.GetProcessFrames(),
                    Input = observedInput.CurrentFrameSequence, Rows = evidence.Count, Overflow = evidenceOverflow
                }));
                foreach (CombatHealthEvidence row in evidence)
                    GD.Print("FIRST_COMBAT_HEALTH_EVENT " + System.Text.Json.JsonSerializer.Serialize(row));
            }
            catch (Exception error)
            {
                GD.Print($"FIRST_COMBAT_HEALTH_OBSERVER_FAULT type={error.GetType().Name} original_preserved=true");
            }
            throw;
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

    private readonly record struct CombatHealthEvidence(string Stage, string Kind, ulong Physics, ulong Process,
        ulong Input, int Hp, int SignalRemaining, int RestoredAmount, float ImpulseX, float ImpulseY, float X, float Y,
        int Damages, int Deaths, int Heals, int Requests, int Recoveries);

    private static void ObserveFailedOriginalSight(PrologueLevelController level, RustShellSquire enemy,
        string observationStage = null)
    {
        // One synchronous diagnostic after the context has already failed. It
        // never selects an actor, authorizes a cast or changes the test verdict.
        var excluded = new Godot.Collections.Array<Rid>();
        using var excludedOwner = (Godot.Collections.Array)excluded;
        AddOwned(enemy);
        using var query = PhysicsRayQueryParameters2D.Create(enemy.GlobalPosition, level.Player.GlobalPosition);
        query.CollideWithAreas = true;
        query.CollideWithBodies = true;
        query.Exclude = excluded;
        using Godot.Collections.Dictionary hit = enemy.GetWorld2D().DirectSpaceState.IntersectRay(query);
        Node collider = hit.Count == 0 ? null : hit["collider"].AsGodotObject() as Node;
        GD.Print("FIRST_COMBAT_FAILED_SIGHT_HIT " + System.Text.Json.JsonSerializer.Serialize(new
        {
            ObservationOnly = true, Observation = "post_failure_synchronous_not_original_refresh",
            Stage = observationStage, Physics = Engine.GetPhysicsFrames(), Original = enemy.GetInstanceId(),
            FromX = enemy.GlobalPosition.X, FromY = enemy.GlobalPosition.Y,
            ToX = level.Player.GlobalPosition.X, ToY = level.Player.GlobalPosition.Y,
            Areas = query.CollideWithAreas, Bodies = query.CollideWithBodies, Mask = query.CollisionMask,
            Excluded = excluded.Count, HitCount = hit.Count, NoHit = hit.Count == 0,
            Collider = collider?.GetInstanceId() ?? 0, Path = ObservedPath(collider),
            Type = collider?.GetType().FullName, Layer = collider is CollisionObject2D body ? body.CollisionLayer : 0,
            BelongsToPlayer = collider != null && RustShellSquire.NodesBelongToSameTarget(collider, level.Player),
            LosNow = hit.Count == 0 || collider != null && RustShellSquire.NodesBelongToSameTarget(collider, level.Player)
        }));
        void AddOwned(Node node)
        {
            if (node is CollisionObject2D body) excluded.Add(body.GetRid());
            using Godot.Collections.Array children = (Godot.Collections.Array)node.GetChildren();
            for (int index = 0; index < children.Count; index++) AddOwned(children[index].AsGodotObject() as Node);
        }
    }

    private static string ObservedPath(Node node)
    {
        if (!GodotObject.IsInstanceValid(node) || !node.IsInsideTree()) return null;
        using NodePath path = node.GetPath();
        return path.ToString();
    }

    private static bool ObserveFailedCombatContext(PrologueLevelController level, string stage,
        PrologueGuidanceInstruction failedInstruction, RustShellSquire expectedOriginal)
    {
        // Observation starts only AFTER the original predicate has failed.
        // Shadow selection is current state, not the earlier Refresh decision.
        ulong physics = Engine.GetPhysicsFrames(), process = Engine.GetProcessFrames();
        try
        {
            CharacterBody2D player = level.Player;
            var input = player.GetNodeOrNull<PlayerInputReader>("Input");
            ulong? sequence = input?.CurrentFrameSequence;
            var guidance = level.GetNode<PrologueGuidanceController>("Guidance");
            var hud = level.GetNodeOrNull<PrologueFormalHud>("Hud/FormalHud");
            Node2D sector = level.CurrentSectorRoot;
            var world = level.GetNodeOrNull<SeamlessPrologueWorld>("WorldRoot/Sectors");
            var group = sector?.GetNodeOrNull<CanyonEncounterGroup>("Gameplay/Encounters/LowCeilingBridgeEncounter");
            Node actors = sector?.GetNodeOrNull("Gameplay/Actors");
            RustShellSquire selected = null;
            if (world?.LayoutVersion == CanyonWorldLayout.CurrentLayoutVersion &&
                GodotObject.IsInstanceValid(group) && GodotObject.IsInstanceValid(actors))
            {
                Godot.Collections.Array<Node> children = actors.GetChildren();
                using Godot.Collections.Array childrenOwner = (Godot.Collections.Array)children;
                foreach (Node child in children)
                    if (child is RustShellSquire candidate && group.OwnsLiveActor(candidate))
                    { selected = candidate; break; }
            }
            else if (world?.LayoutVersion != CanyonWorldLayout.CurrentLayoutVersion)
                selected = actors?.GetNodeOrNull<RustShellSquire>("RustShellSquire");
            bool hasActor = GodotObject.IsInstanceValid(selected);
            Vector2 separation = hasActor ? selected.GlobalPosition - player.GlobalPosition : Vector2.Zero;
            Vector2 canvas = hasActor ? selected.GetGlobalTransformWithCanvas().Origin : Vector2.Zero;
            GD.Print("FIRST_COMBAT_FAILED_CONTEXT " + System.Text.Json.JsonSerializer.Serialize(new
            {
                Observation = "post_failure_synchronous_not_original_refresh", Stage = stage,
                PhysicsStart = physics, ProcessStart = process, InputSequenceStart = sequence,
                OriginalRefreshRange = "unknown", Guidance = guidance.GuidanceEnabled,
                failedInstruction.Visible, failedInstruction.StepText, failedInstruction.ContextTitle,
                Language = guidance.Language.ToString(), Paused = level.GetTree().Paused,
                Sector = level.CurrentSector.ToString(), SectorRoot = sector?.GetInstanceId(),
                Layout = world?.LayoutVersion, HudPresent = hud != null,
                Recovery = hud?.CurrentModel.RecoveryVisible, Completion = hud?.CurrentModel.CompletionVisible,
                Boss = hud?.CurrentModel.BossVisible,
                Player = player.GetInstanceId(), PlayerVisible = player.IsVisibleInTree(),
                PlayerInside = player.IsInsideTree(), PlayerQueued = player.IsQueuedForDeletion(),
                PlayerPhysics = player.IsPhysicsProcessing(), PlayerCanProcess = player.CanProcess(),
                PlayerHp = player.GetNodeOrNull<HealthComponent>("Health")?.CurrentHealth,
                Mode = player.GetNodeOrNull<PlayerActionModeController>("ActionModes")?.CurrentMode.ToString(),
                Claw = player.GetNodeOrNull<MechanicalClawController>("MechanicalClaw")?.CurrentFrame.State.ToString(),
                PlayerX = player.GlobalPosition.X, PlayerY = player.GlobalPosition.Y,
                PlayerVx = player.Velocity.X, PlayerVy = player.Velocity.Y,
                GroupPresent = GodotObject.IsInstanceValid(group), GroupActive = group?.IsActive,
                GroupComplete = group?.IsComplete, GroupBatch = group?.CurrentBatch,
                GroupLive = group?.LiveActorCount,
                ExpectedOriginal = GodotObject.IsInstanceValid(expectedOriginal) ? expectedOriginal.GetInstanceId() : (ulong?)null,
                ExpectedOwnedNow = GodotObject.IsInstanceValid(group) && GodotObject.IsInstanceValid(expectedOriginal)
                    ? group.OwnsLiveActor(expectedOriginal) : (bool?)null,
                ShadowSelectedOriginalNow = hasActor ? selected.GetInstanceId() : (ulong?)null,
                SelectedEqualsExpected = hasActor && GodotObject.IsInstanceValid(expectedOriginal)
                    ? selected == expectedOriginal : (bool?)null,
                Resident = hasActor ? selected.HasNode("ResidentBinding") : (bool?)null,
                EnemyVisible = hasActor ? selected.IsVisibleInTree() : (bool?)null,
                EnemyPhysicsAuxiliary = hasActor ? selected.IsPhysicsProcessing() : (bool?)null,
                EnemyHp = hasActor ? selected.GetNodeOrNull<HealthComponent>("Health")?.CurrentHealth : null,
                EnemyX = hasActor ? selected.GlobalPosition.X : (float?)null,
                EnemyY = hasActor ? selected.GlobalPosition.Y : (float?)null,
                EnemyVx = hasActor ? selected.Velocity.X : (float?)null,
                EnemyVy = hasActor ? selected.Velocity.Y : (float?)null,
                Dx = hasActor ? separation.X : (float?)null, Dy = hasActor ? separation.Y : (float?)null,
                InsideEnter340Now = hasActor ? Mathf.Abs(separation.X) <= 340 : (bool?)null,
                InsideRetain400Now = hasActor ? Mathf.Abs(separation.X) <= 400 : (bool?)null,
                SameHeight100Now = hasActor ? Mathf.Abs(separation.Y) <= 100 : (bool?)null,
                CanvasX = hasActor ? canvas.X : (float?)null, CanvasY = hasActor ? canvas.Y : (float?)null,
                InViewportNow = hasActor ? level.GetViewportRect().HasPoint(canvas) : (bool?)null
            }));
            if (hasActor) ObserveFailedOriginalSight(level, selected, stage);
            else GD.Print("FIRST_COMBAT_FAILED_SIGHT_UNKNOWN reason=no_current_owned_actor");
            GD.Print("FIRST_COMBAT_FAILED_CONTEXT_END " + System.Text.Json.JsonSerializer.Serialize(new
            {
                Observation = "post_failure_synchronous_not_original_refresh", Stage = stage,
                PhysicsStart = physics, PhysicsEnd = Engine.GetPhysicsFrames(),
                ProcessStart = process, ProcessEnd = Engine.GetProcessFrames(),
                InputSequenceStart = sequence, InputSequenceEnd = input?.CurrentFrameSequence
            }));
            return true;
        }
        catch (Exception error)
        {
            GD.Print("FIRST_COMBAT_FAILED_CONTEXT_UNKNOWN " + System.Text.Json.JsonSerializer.Serialize(new
            { Stage = stage, PhysicsStart = physics, Type = error.GetType().Name, error.Message }));
            return false;
        }
    }

    public static void AssertCombatGuidance(PrologueLevelController level, string stage,
        PrologueObjective expectedObjective = PrologueObjective.LearnGrappleMode,
        RustShellSquire originalEnemy = null)
    {
        PrologueGuidanceInstruction instruction = level.GetNode<PrologueGuidanceController>("Guidance").CurrentInstruction;
        Label ribbon = level.GetNode<Label>("Hud/FormalHud/ObjectiveRibbon/Text");
        PrologueObjective objective = level.GetNode<PrologueCompletionController>("Completion").CurrentObjective;
        GD.Print($"FIRST_COMBAT_GUIDANCE stage={stage} objective={objective} step={instruction.StepText} ribbon={ribbon.Text}");
        TestAssert.Equal(expectedObjective, objective,
            "contextual combat guidance does not consume or replace the underlying mechanical-grapple objective");
        bool hasCombat = instruction.Visible && HasCurrentCombatControls(instruction.StepText,
            level.GetNode<PrologueGuidanceController>("Guidance").Language);
        if (!hasCombat) ObserveFailedCombatContext(level, stage, instruction, originalEnemy);
        TestAssert.True(hasCombat,
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
        CharacterBody2D player, RustShellSquire enemy, Action<string> stage)
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
            TestAssert.True(Math.Abs(Math.Abs(enemy.GlobalPosition.X - player.GlobalPosition.X) - sample.Item1) < 12f,
                "scheduled actor motion stays within the arranged hysteresis test margin");
            if (sample.Item2) AssertCombatGuidance(level, $"hysteresis distance {sample.Item1}");
            else AssertOrdinary(level, ordinary, $"hysteresis distance {sample.Item1}");
        }
        await PlaceEnemy(tree, player, enemy, 210f);
        enemy.Visible = false;
        await Settle(tree);
        AssertOrdinary(level, ordinary, "hidden enemy");
        // A real existing negative predicate exercises the observer without
        // inventing an actor, changing the verdict or waiting another frame.
        PrologueGuidanceInstruction hidden = guidance.CurrentInstruction;
        var input = player.GetNode<PlayerInputReader>("Input");
        var snapshot = (Engine.GetPhysicsFrames(), Engine.GetProcessFrames(), input.CurrentFrameSequence,
            player.GetInstanceId(), player.GlobalPosition, player.Velocity,
            enemy.GetInstanceId(), enemy.GlobalPosition, enemy.Velocity);
        TestAssert.True(!HasCurrentCombatControls(hidden.StepText, guidance.Language),
            "real hidden-enemy control first establishes the failed combat predicate");
        TestAssert.True(ObserveFailedCombatContext(level, "controlled hidden predicate", hidden, enemy),
            "the actual saved-actor negative control completes its diagnostic snapshot without fault");
        TestAssert.True(snapshot == (Engine.GetPhysicsFrames(), Engine.GetProcessFrames(), input.CurrentFrameSequence,
                player.GetInstanceId(), player.GlobalPosition, player.Velocity,
                enemy.GetInstanceId(), enemy.GlobalPosition, enemy.Velocity) &&
            guidance.CurrentInstruction.Equals(hidden),
            "synchronous diagnostic cannot advance frames/input or mutate actors or the displayed instruction");
        GD.Print("FIRST_COMBAT_SNAPSHOT_CONTROL_OK actual_hidden_actor=true failed_predicate=true synchronous=true unchanged=true original_assertions_retained=true");
        enemy.Visible = true;
        stage("hidden-restored");
        await Settle(tree);
        AssertCombatGuidance(level, "visible enemy restored", originalEnemy: enemy);
        stage("different-height");
        await PlaceEnemy(tree, player, enemy, 210f, -140f);
        AssertOrdinary(level, ordinary, "enemy on a different height layer");
        stage("same-height-restored");
        await PlaceEnemy(tree, player, enemy, 210f);
        AssertCombatGuidance(level, "same floor restored");

        var wall = new StaticBody2D { Name = "GuidanceSightTestWall", CollisionLayer = CombatCollisionLayers.World };
        wall.AddChild(new CollisionShape2D { Shape = new RectangleShape2D { Size = new Vector2(20, 180) } });
        level.AddChild(wall);
        wall.GlobalPosition = player.GlobalPosition + new Vector2(-100, -30);
        try
        {
            stage("wall-present");
            await Settle(tree);
            TestAssert.True(!enemy.HasLineOfSightTo(player), "the temporary world wall really blocks enemy sight");
            AssertOrdinary(level, ordinary, "world wall blocks encounter");
        }
        finally { stage("wall-removal"); wall.QueueFree(); await Settle(tree); }
        AssertCombatGuidance(level, "wall removed");

        stage("terrain-attachment");
        await RealTerrainAttachment(tree, level);
        stage("terrain-attachment-restored");
        await PlaceEnemy(tree, player, enemy, 210f);
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
        await CheckLayoutVariants(tree, level, player, enemy, stage);

        stage("original-enemy-death");
        HealthComponent health = enemy.GetNode<HealthComponent>(enemy.HealthPath);
        TestAssert.True(health.ApplyDamage(new DamageRequest(health.CurrentHealth, Vector2.Zero, 0f, 991UL)),
            "controlled lethal damage is accepted by the real first enemy health component");
        await Settle(tree);
        var firstGroup = level.CurrentSectorRoot.GetNode<CanyonEncounterGroup>("Gameplay/Encounters/LowCeilingBridgeEncounter");
        int liveResidents = level.CurrentSectorRoot.GetNode("Gameplay/Actors").GetChildren().OfType<RustShellSquire>()
            .Count(node => node.HasNode("ResidentBinding") && node.IsVisibleInTree() &&
                node.GetNode<HealthComponent>("Health").CurrentHealth > 0);
        GD.Print($"FIRST_COMBAT_ORIGINAL_DEATH_FACTS complete={firstGroup.IsComplete} " +
            $"original_live={firstGroup.LiveActorCount} visible_living_residents={liveResidents} " +
            $"context={level.GetNode<PrologueGuidanceController>("Guidance").CurrentInstruction.ContextTitle}");
        TestAssert.True(firstGroup.IsComplete && firstGroup.LiveActorCount == 0 && liveResidents > 0,
            "original encounter is genuinely defeated while its separate resident population stays alive and visible");
        AssertOrdinary(level, ordinary, "dead enemy ends contextual teaching");
    }

    private static async Task CheckRepeatedFailureCard(SceneTree tree, PrologueLevelController level,
        CharacterBody2D player, ulong originalEnemyId, Action<string> stage)
    {
        var guidance = level.GetNode<PrologueGuidanceController>("Guidance");
        var visual = level.GetNode<WorldTargetMarkerVisual>("Hud/Guidance/VisualRoot");
        var sector = level.CurrentSector;
        int recoveries = 0, requests = 0, round = -1, observations = 0, observationFaults = 0;
        bool commitsValid = true;
        ulong startPhysics = 0, startMs = 0;
        void Observe(string stage)
        {
            // Scalar event/boundary observations, never an input or transaction.
            if (observations >= 32) return;
            observations++;
            try
            {
                GD.Print("FIRST_COMBAT_RECOVERY_BOUNDARY " + System.Text.Json.JsonSerializer.Serialize(
                    CaptureRetrySnapshot(level, player, round, stage, requests, recoveries, startPhysics, startMs)));
            }
            catch (Exception error)
            {
                observationFaults++;
                GD.Print($"FIRST_COMBAT_RECOVERY_OBSERVATION_FAULT type={error.GetType().Name} faults={observationFaults}");
            }
        }
        void OnRequest(string reason) { if (reason == "DEATH RESET") { requests++; Observe("requested"); } }
        void OnRecovery(string reason)
        {
            if (reason != "DEATH RESET") return;
            recoveries++;
            try
            {
                commitsValid &= !level.ResidentPreparationPending && !level.RecoveryInProgress &&
                    level.ResidentPreparationFailure.Length == 0 &&
                    level.GetNode<PrologueGameFlowController>("GameFlow").State == PrologueGameFlowState.Playing &&
                    level.GetNode<CanyonResidentCoordinator>("ResidentCoordinator").IsPrepared &&
                    level.Player == player && player.GetNode<HealthComponent>("Health").CurrentHealth == 5;
            }
            catch { commitsValid = false; }
            Observe("completed");
        }
        level.RecoveryRequested += OnRequest;
        level.RecoveryCompleted += OnRecovery;
        try
        {
            for (int i = 0; i < 2; i++)
            {
                stage($"retry{i}-before-frame");
                await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
                round = i;
                startPhysics = Engine.GetPhysicsFrames(); startMs = Time.GetTicksMsec();
                Observe("before-request");
                stage($"retry{i}-request");
                level.RequestRespawn("DEATH RESET");
                Observe("after-request");
                int waited = await ObserveRetryWait(() => WaitForRetryRecovery(tree, level, player, sector, i + 1,
                    () => requests, () => recoveries, () => commitsValid), () => Observe("recovery-wait-failed"));
                Observe("after-completion-wait");
                TestAssert.Equal(i + 1, recoveries, "each real same-sector recovery completes before requesting another");
                TestAssert.Equal(sector, level.CurrentSector, "failure-card fixture remains in the original sector");
                GD.Print($"FIRST_COMBAT_RECOVERY_COMMIT_OK round={i} requested={requests} completed={recoveries} " +
                    $"wait_physics={waited} cap=180 same_player=true same_sector=true event_committed=true hp=5");
            }
        }
        finally
        {
            level.RecoveryRequested -= OnRequest;
            level.RecoveryCompleted -= OnRecovery;
            GD.Print($"FIRST_COMBAT_RECOVERY_OBSERVATION_END rows={observations}/32 faults={observationFaults} wait_contract=real-completion additional_recovery_requests=0");
        }

        // This is a controlled context arrangement, NOT an input-route traversal.
        // Recovery retires actors at start160,600, outside the encounter activation.
        stage("retry-reactivation");
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

    private sealed class RetryRecoveryWaitFailure : InvalidOperationException
    {
        internal string Code { get; }
        internal RetryRecoveryWaitFailure(string code) : base("retry recovery wait: " + code) { Code = code; }
    }

    private static async Task<int> ObserveRetryWait(Func<Task<int>> wait, Action failureSnapshot)
    {
        try { return await wait(); }
        catch
        {
            // Diagnostics cannot replace the transaction failure, including when
            // a fixture has been freed or a diagnostic consumer itself throws.
            try { failureSnapshot(); }
            catch (Exception error)
            {
                GD.Print($"FIRST_COMBAT_RECOVERY_SNAPSHOT_OBSERVER_FAULT type={error.GetType().Name} original_preserved=true");
            }
            throw;
        }
    }

    private static Dictionary<string, object> CaptureRetrySnapshot(PrologueLevelController level,
        CharacterBody2D player, int round, string stage, int requests, int completions,
        ulong startPhysics, ulong startMs)
    {
        bool levelValid = GodotObject.IsInstanceValid(level);
        var faults = new List<string>();
        var row = new Dictionary<string, object>
        {
            ["Round"] = round, ["Stage"] = stage, ["Physics"] = Engine.GetPhysicsFrames(),
            ["PhysicsElapsed"] = Engine.GetPhysicsFrames() - startPhysics,
            ["ElapsedMs"] = Time.GetTicksMsec() - startMs, ["Requests"] = requests,
            ["Completions"] = completions, ["LevelValid"] = levelValid,
            ["observation_only"] = true, ["wait_contract"] = "real-completion",
            ["additional_recovery_requests"] = 0
        };
        void Field(string name, Func<object> read)
        {
            try { row[name] = read(); }
            catch (Exception error) { row[name] = null; faults.Add(name + ":" + error.GetType().Name); }
        }
        Field("LevelInsideTree", () => levelValid && level.IsInsideTree());
        Field("State", () => levelValid ? level.GetNodeOrNull<PrologueGameFlowController>("GameFlow")?.State.ToString() : null);
        Field("Pending", () => levelValid ? level.ResidentPreparationPending : null);
        Field("Failure", () => levelValid ? level.ResidentPreparationFailure : null);
        Field("Recovering", () => levelValid ? level.RecoveryInProgress : null);
        Field("Rejected", () => levelValid ? level.GetNodeOrNull<PrologueGameFlowController>("GameFlow")?.LastRejectedTransition : null);
        Field("Prepared", () => levelValid ? level.GetNodeOrNull<CanyonResidentCoordinator>("ResidentCoordinator")?.IsPrepared : null);
        Field("Epoch", () => levelValid ? level.Residents?.Epoch : null);
        Field("Actors", () => levelValid ? level.Residents?.TrackedActorCount : null);
        Field("Sector", () => levelValid ? level.CurrentSector?.ToString() : null);
        Field("SectorId", () => levelValid && GodotObject.IsInstanceValid(level.CurrentSectorRoot)
            ? level.CurrentSectorRoot.GetInstanceId() : 0UL);
        Field("SamePlayer", () => levelValid && level.Player == player);
        Field("PlayerId", () => levelValid && GodotObject.IsInstanceValid(level.Player) ? level.Player.GetInstanceId() : 0UL);
        Field("PlayerProcessing", () => levelValid && GodotObject.IsInstanceValid(level.Player) ? level.Player.CanProcess() : null);
        Field("Hp", () => levelValid && GodotObject.IsInstanceValid(level.Player)
            ? level.Player.GetNodeOrNull<HealthComponent>("Health")?.CurrentHealth : null);
        row["ReadFaults"] = faults.ToArray();
        return row;
    }

    private static async Task<int> WaitForRetryRecovery(SceneTree tree, PrologueLevelController level,
        CharacterBody2D player, PrologueSectorId? sector, int expected, Func<int> requested,
        Func<int> completed, Func<bool> committedAtEvent)
    {
        // This consumes the real transaction, never input-sample waits while input
        // is disabled. Same finite180-physics contract as the existing flow tests.
        if (requested() != expected) throw new RetryRecoveryWaitFailure("request-not-accepted");
        for (int waited = 0; ; waited++)
        {
            if (requested() != expected) throw new RetryRecoveryWaitFailure("request-count-changed");
            int count = completed();
            if (count < expected - 1 || count > expected) throw new RetryRecoveryWaitFailure("completion-count");
            if (!GodotObject.IsInstanceValid(level) || !level.IsInsideTree() || level.IsQueuedForDeletion() ||
                !GodotObject.IsInstanceValid(player) || !player.IsInsideTree() || player.IsQueuedForDeletion() ||
                level.Player != player) throw new RetryRecoveryWaitFailure("player-or-level-lost");
            if (level.ResidentPreparationFailure.Length != 0) throw new RetryRecoveryWaitFailure("preparation-failed");
            if (!committedAtEvent()) throw new RetryRecoveryWaitFailure("completion-not-committed");
            var flow = level.GetNode<PrologueGameFlowController>("GameFlow");
            if (count == expected)
            {
                if (flow.State != PrologueGameFlowState.Playing || level.ResidentPreparationPending ||
                    level.RecoveryInProgress || !level.GetNode<CanyonResidentCoordinator>("ResidentCoordinator").IsPrepared ||
                    level.CurrentSector != sector || player.GetNode<HealthComponent>("Health").CurrentHealth != 5)
                    throw new RetryRecoveryWaitFailure("completion-not-committed");
                return waited;
            }
            if (!level.RecoveryInProgress) throw new RetryRecoveryWaitFailure("recovery-owner-lost");
            if (!level.ResidentPreparationPending) throw new RetryRecoveryWaitFailure("preparation-owner-lost");
            if (flow.State != PrologueGameFlowState.Recovering) throw new RetryRecoveryWaitFailure("flow-left-recovery");
            if (waited == 180) throw new RetryRecoveryWaitFailure("completion-timeout");
            await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
        }
    }

    private static async Task ExpectRecoveryWaitRejection(SceneTree tree, PrologueLevelController level,
        CharacterBody2D player, Func<int> requested, Func<int> completed, string code, bool immediate)
    {
        ulong before = Engine.GetPhysicsFrames();
        ulong beforeMs = Time.GetTicksMsec();
        int beforeRequests = requested(), beforeCompletions = completed(), snapshotCalls = 0;
        Dictionary<string, object> snapshot = null;
        bool rejected = false;
        PrologueSectorId? sector = GodotObject.IsInstanceValid(level) ? level.CurrentSector : null;
        try
        {
            await ObserveRetryWait(() => WaitForRetryRecovery(tree, level, player, sector, 1, requested, completed, () => true), () =>
            {
                snapshotCalls++;
                snapshot = CaptureRetrySnapshot(level, player, -1, "recovery-wait-failed", requested(), completed(), before, beforeMs);
                GD.Print("FIRST_COMBAT_RECOVERY_FAILURE_SNAPSHOT " + System.Text.Json.JsonSerializer.Serialize(snapshot));
                if (code == "recovery-owner-lost") throw new InvalidOperationException("intentional snapshot observer failure");
            });
        }
        catch (RetryRecoveryWaitFailure failure) when (failure.Code == code) { rejected = true; }
        ulong elapsed = Engine.GetPhysicsFrames() - before;
        TestAssert.True(rejected && (!immediate || elapsed == 0) && elapsed < 180,
            "FIRST_COMBAT_RECOVERY_REJECTS_" + code + " without blind waiting");
        TestAssert.True(snapshotCalls == 1 && snapshot != null && snapshot.ContainsKey("Failure") &&
            snapshot.ContainsKey("Rejected") && snapshot.ContainsKey("SectorId") && snapshot.ContainsKey("PlayerId") &&
            snapshot.ContainsKey("Prepared") && snapshot.ContainsKey("PhysicsElapsed") &&
            (string)snapshot["Stage"] == "recovery-wait-failed" && (int)snapshot["Requests"] == beforeRequests &&
            (int)snapshot["Completions"] == beforeCompletions && requested() == beforeRequests && completed() == beforeCompletions,
            "FIRST_COMBAT_RECOVERY_FAILURE_SNAPSHOT preserves failure-boundary state and event consumption");
        TestAssert.Equal(code != "player-or-level-lost", (bool)snapshot["LevelValid"],
            "failure snapshot safely preserves liveness even after real fixture disposal");
        GD.Print($"FIRST_COMBAT_RECOVERY_REJECT_OK code={code} wait_physics={elapsed} cap=180 immediate={immediate} fake_events=0");
    }

    private static async Task CheckRecoveryWaitRejections(SceneTree tree)
    {
        // These are terminal isolated fixtures, not ordinary route traversal.
        for (int mode = 0; mode < 2; mode++)
        {
            var level = CreateLevelFixture();
            var player = level.GetNode<CharacterBody2D>("PlayerRoot/Player");
            var flow = level.GetNode<PrologueGameFlowController>("GameFlow");
            var world = level.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors");
            int requests = 0, completions = 0;
            bool cancelQueued = false, cancelOnReset = false, observedReset = false;
            void Requested(string _)
            {
                requests++;
                if (cancelQueued) level.CancelQueuedRecovery();
            }
            void Completed(string _) => completions++;
            void ResetFinished(PrologueSector sector, bool success)
            {
                if (!cancelOnReset || !success || sector != level.CurrentSectorRoot) return;
                TestAssert.True(flow.State == PrologueGameFlowState.Recovering &&
                    level.RecoveryInProgress && level.ResidentPreparationPending,
                    "owner-loss control occurs inside actual deferred resident preparation");
                observedReset = true;
                level.CancelResidentPreparation();
            }
            level.RecoveryRequested += Requested;
            level.RecoveryCompleted += Completed;
            world.ResidentSectorResetFinished += ResetFinished;
            tree.Root.AddChild(level);
            try
            {
                for (int frame = 0; frame < 180 && flow.State != PrologueGameFlowState.Playing; frame++)
                    await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
                TestAssert.Equal(PrologueGameFlowState.Playing, flow.State, "recovery rejection fixture really starts Playing");
                if (mode == 0)
                {
                    level.BeginResidentPreparation();
                    level.RequestRespawn("DEATH RESET");
                    TestAssert.True(requests == 0 && completions == 0 && level.ResidentPreparationPending &&
                        !level.RecoveryInProgress, "real pending-preparation request is rejected before publication");
                    await ExpectRecoveryWaitRejection(tree, level, player, () => requests, () => completions,
                        "request-not-accepted", immediate: true);
                    // Only another terminal control follows, never normal gameplay.
                    level.CancelResidentPreparation();
                    cancelQueued = true;
                    level.RequestRespawn("DEATH RESET");
                    TestAssert.True(requests == 1 && completions == 0 && level.ResidentPreparationPending &&
                        !level.RecoveryInProgress && flow.State == PrologueGameFlowState.Recovering,
                        "synchronous Requested cancellation really clears queued recovery ownership");
                    await ExpectRecoveryWaitRejection(tree, level, player, () => requests, () => completions,
                        "recovery-owner-lost", immediate: true);
                }
                else
                {
                    cancelOnReset = true;
                    level.RequestRespawn("DEATH RESET");
                    await ExpectRecoveryWaitRejection(tree, level, player, () => requests, () => completions,
                        "preparation-owner-lost", immediate: false);
                    TestAssert.True(observedReset && requests == 1 && completions == 0,
                        "actual reset callback cancels owned preparation without fabricating completion");
                }
            }
            finally
            {
                level.RecoveryRequested -= Requested;
                level.RecoveryCompleted -= Completed;
                world.ResidentSectorResetFinished -= ResetFinished;
                level.CancelQueuedRecovery();
                level.CancelResidentPreparation();
                await TestSceneTeardown.FreeSceneAfterAudioDrain(tree, level);
            }
            if (mode == 0)
                await ExpectRecoveryWaitRejection(tree, level, player, () => requests, () => completions,
                    "player-or-level-lost", immediate: true);
        }
    }

    private static async Task CheckLayoutVariants(SceneTree tree, PrologueLevelController level,
        CharacterBody2D player, RustShellSquire enemy, Action<string> stage)
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
                stage($"layout-{size}-{language}-{scale}");
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
            stage("layout-restoration");
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
        var guidance = level.GetNode<PrologueGuidanceController>("Guidance");
        CharacterBody2D player = level.Player;
        var motor = player.GetNode<PlayerMotor>("Motor");
        PrologueObjective phase = level.GetNode<PrologueCompletionController>("Completion").CurrentObjective;
        // This suite has never requested a jet. After the combat card withdraws,
        // the new, lower-priority first-use lesson may legitimately own chrome;
        // the underlying ordinary objective must still be unchanged.
        bool firstJet = phase is (PrologueObjective.Wake or PrologueObjective.LearnAttackMode or PrologueObjective.ReachBasin) &&
            guidance.GuidanceEnabled && !level.GetTree().Paused &&
            level.GetNode<PrologueGameFlowController>("GameFlow").State == PrologueGameFlowState.Playing &&
            level.CurrentSector == PrologueSectorId.CanyonEntrance && player.IsVisibleInTree() &&
            player.IsPhysicsProcessing() && motor.IsPhysicsProcessing() && motor.CanProcess() &&
            player.IsOnFloor() && motor.ThrusterState.BurnSerial == 0UL &&
            player.GetNode<HealthComponent>("Health").CurrentHealth > 0 &&
            player.GetNode<MechanicalClawController>("MechanicalClaw").CurrentFrame.State == MechanicalClawState.Idle &&
            !player.GetNode<PlayerCombat>("Combat").IsAttacking &&
            !player.GetNode<PlayerShieldController>("Shield").IsActive &&
            !player.GetNode<PlayerInputReader>("Input").RawFrame.ShieldHeld &&
            !formal.CurrentModel.RecoveryVisible && !formal.CurrentModel.CompletionVisible;
        string jetTitle = guidance.Language == GameLanguage.English ? "ECHO · THRUSTER" : "ECHO · 推进器";
        TestAssert.Equal(objective, formal.CurrentModel.Objective, "ordinary HUD model preserved: " + stage);
        TestAssert.Equal(firstJet ? jetTitle : objective, formal.GetNode<Label>("ObjectiveRibbon/Text").Text,
            "actual ribbon shows the eligible first-use lesson or restores the ordinary title: " + stage);
        TestAssert.Equal(firstJet ? jetTitle : string.Empty, guidance.CurrentInstruction.ContextTitle,
            "combat card is withdrawn without discarding a valid first-use lesson: " + stage);
        if (firstJet)
            TestAssert.True((phase == PrologueObjective.ReachBasin
                    ? guidance.CurrentInstruction.Targets.Count == 1 &&
                        guidance.CurrentInstruction.Targets[0] == PrologueGuidanceTargetKey.EntranceExit
                    : guidance.CurrentInstruction.Targets.Count == 0) &&
                guidance.CurrentInstruction.StepText.Contains(guidance.Language == GameLanguage.English ? "SHORT JET" : "短喷"),
                "unused first jet preserves required forward guidance without a stale enemy card: " + stage);
    }

    private static async Task PlaceEnemy(SceneTree tree, CharacterBody2D player, RustShellSquire enemy,
        float distance, float height = 0f)
    {
        // Controlled UI predicates only: the right-hand lane contains a real
        // resident body. Keep horizontal/range cases on the other side rather
        // than removing that resident or bypassing native LOS. The distinct
        // height-negative case retains its clear right-hand vertical column.
        enemy.GlobalPosition = player.GlobalPosition + new Vector2(height == 0f ? -distance : distance, height);
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
        string originalName = group.EncounterId.ToString().Replace('.', '_') + "_batch0_1";
        RustShellSquire[] carriers = group.GetNode(group.ActorRootPath).GetChildren().OfType<RustShellSquire>().ToArray();
        RustShellSquire enemy = carriers.Single(node => GodotObject.IsInstanceValid(node) && node.IsInsideTree() &&
            !node.IsQueuedForDeletion() && node.Name.ToString() == originalName && !node.HasNode("ResidentBinding"));
        TestAssert.True(enemy.SceneFilePath == "res://scenes/enemies/RustShellSquire.tscn" &&
            enemy.GetNode<HealthComponent>("Health").CurrentHealth == 3,
            "unique genuine saved Rust actor has serialized three HP, not the guidance-only Marker");
        GD.Print($"FIRST_COMBAT_ORIGINAL_ACTOR_SELECTED original_wave=true resident_carriers={carriers.Count(node => node.HasNode("ResidentBinding"))} actor={enemy.GetInstanceId()}");
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
            enemy.GlobalPosition = player.GlobalPosition + new Vector2(210f, 0f);
            await Settle(tree);
            await KeyboardActionRuntimeTests.Frames(tree, input, 30);
            await PlaceEnemy(tree, player, enemy, 330f);
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
