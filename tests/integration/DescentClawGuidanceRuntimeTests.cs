using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Knight.Abilities;
using Knight.Combat;
using Knight.Player;
using Knight.Prologue;
using Knight.Prologue.Guidance;
using Knight.Prologue.Seamless;
using Knight.Settings;
using Knight.Ui;

namespace Knight.Tests.Integration;

public static class DescentClawGuidanceRuntimeTests
{
    private static bool TracingDefinitions =>
        Array.Exists(OS.GetCmdlineUserArgs(), value => value == "--trace-sector-definitions");

    // Opt-in diagnosis only. Scalar values never retain/clone/dispose loaded
    // definitions across await, and add no extra Load, GC or frame wait.
    internal static void TraceSectorDefinitions(SeamlessPrologueWorld world, string stage)
    {
        if (!TracingDefinitions) return;
        var rows = new List<string>();
        int count = world.Definitions?.Count ?? -1;
        for (int index = 0; index < count; index++)
        {
            PrologueSectorDefinition definition = world.Definitions[index];
            bool valid = GodotObject.IsInstanceValid(definition);
            rows.Add($"{index}:null={definition == null},valid={valid},id={(valid ? definition.GetInstanceId() : 0UL)}");
        }
        GD.Print($"SECTOR_DEFINITION_IDENTITY_DIAGNOSTIC stage={stage} world={world.GetInstanceId()} physics={Engine.GetPhysicsFrames()} count={count} items=[{string.Join(';', rows)}] retained=false cache_changed=false");
    }

    internal static PrologueLevelController CreateLevelFixture()
    {
        var level = InstantiateLevelFixture();
        var flow = level.GetNode<PrologueGameFlowController>("GameFlow");
        flow.HeadlessAutoStart = false;
        flow.AutomationAutoStart = true;
        var world = level.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors");
        TraceSectorDefinitions(world, "descent.after-instantiate");
        world.LayoutVersion = 2;
        world.InitialSector = PrologueSectorId.CanyonDescent;
        level.GetNode<CharacterBody2D>("PlayerRoot/Player").Position = new Vector2(1600, 1040);
        IsolatedPrologueFixture.Configure(level, "descent-claw-guidance");
        return level;
    }

    private static PrologueLevelController InstantiateLevelFixture()
    {
        // Keep the ordinary load/instantiate expression unchanged. Only the
        // opt-in diagnostic keeps this same packed scene for paired getters;
        // no additional load, cache mode, clone or reference crosses await.
        if (!TracingDefinitions)
            return GD.Load<PackedScene>("res://scenes/prologue/PrologueLevel.tscn")
                .Instantiate<PrologueLevelController>();
        PackedScene packed = GD.Load<PackedScene>("res://scenes/prologue/PrologueLevel.tscn");
        TracePackedDefinitions(packed, "descent.source-pre");
        PrologueLevelController level = packed.Instantiate<PrologueLevelController>();
        TraceSectorDefinitions(level.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors"),
            "descent.instance-immediate");
        TracePackedDefinitions(packed, "descent.source-post");
        return level;
    }

    private static void TracePackedDefinitions(PackedScene packed, string stage)
    {
        // GetState is a read-only view of the same packed source, not an owned
        // clone. Do not Dispose borrowed SceneState/Resource wrappers. The
        // added getters can perturb wrapper lifetime: this is diagnosis only,
        // never proof of unchanged timing or native-slot health.
        SceneState state = packed.GetState();
        int nodeMatches = 0, propertyMatches = 0, count = -1;
        var rows = new List<string>();
        Variant.Type kind = Variant.Type.Nil;
        for (int node = 0; node < state.GetNodeCount(); node++)
        {
            using NodePath path = state.GetNodePath(node);
            if (path.ToString() is not ("WorldRoot/Sectors" or "./WorldRoot/Sectors")) continue;
            nodeMatches++;
            for (int property = 0; property < state.GetNodePropertyCount(node); property++)
            {
                using StringName name = state.GetNodePropertyName(node, property);
                if (name.ToString() != "Definitions") continue;
                propertyMatches++;
                using Variant value = state.GetNodePropertyValue(node, property);
                kind = value.VariantType;
                if (kind != Variant.Type.Array) continue;
                using Godot.Collections.Array values = value.AsGodotArray();
                count = values.Count;
                for (int index = 0; index < count; index++)
                {
                    using Variant item = values[index];
                    GodotObject definition = item.VariantType == Variant.Type.Object
                        ? item.AsGodotObject() : null;
                    bool valid = GodotObject.IsInstanceValid(definition);
                    rows.Add($"{index}:type={item.VariantType},null={definition == null},valid={valid},id={(valid ? definition.GetInstanceId() : 0UL)}");
                }
            }
        }
        GD.Print($"SECTOR_PACKED_DEFINITION_DIAGNOSTIC stage={stage} packed={packed.GetInstanceId()} " +
            $"state={state.GetInstanceId()} nodes={nodeMatches} properties={propertyMatches} type={kind} " +
            $"count={count} items=[{string.Join(';', rows)}] retained_across_await=false extra_load=false getter_perturbation_possible=true");
        TestAssert.True(nodeMatches == 1 && propertyMatches == 1 && kind == Variant.Type.Array && count == 5,
            "source diagnostic locates the unique saved Definitions export without changing it");
    }

    public static async Task Run(SceneTree tree, bool presentation = false)
    {
        var level = CreateLevelFixture();
        int recoveries = 0, requests = 0, attacks = 0;
        void Recovered(string _) => recoveries++;
        void Requested(string _) => requests++;
        level.RecoveryCompleted += Recovered;
        level.RecoveryRequested += Requested;
        TraceSectorDefinitions(level.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors"), "descent.before-add-child");
        tree.Root.AddChild(level);
        var player = level.Player;
        var input = player.GetNode<PlayerInputReader>("Input");
        var claw = player.GetNode<MechanicalClawController>("MechanicalClaw");
        var combat = player.GetNode<PlayerCombat>("Combat");
        void Attacked(ulong _, int __) => attacks++;
        combat.AttackStarted += Attacked;
        try
        {
            var flow = level.GetNode<PrologueGameFlowController>("GameFlow");
            for (int frame = 0; frame < 60 && flow.State != PrologueGameFlowState.Playing; frame++)
                await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            TestAssert.Equal(PrologueGameFlowState.Playing, flow.State, "real isolated startup completes");
            await KeyboardActionRuntimeTests.Frames(tree, input, 8);
            var sector = level.CurrentSectorRoot;
            var floor = sector.GetNode<StaticBody2D>("Gameplay/Collision/RiftLowerCatch");
            var target = sector.GetNode<GrappleSurface2D>("Gameplay/Collision/RiftMiddleCatch/GrappleSurfaceProfile");
            var completion = level.GetNode<PrologueCompletionController>("Completion");
            var guidance = level.GetNode<PrologueGuidanceController>("Guidance");
            ulong playerId = player.GetInstanceId();
            TestAssert.True(level.CurrentSector == PrologueSectorId.CanyonDescent && Supported(player, floor) &&
                player.GlobalPosition.DistanceTo(new Vector2(1600, 1040)) < 1,
                "saved V2 second-layer player has real LowerCatch support, not a legacy first-shelf arrangement");
            var bodyCollider = player.GetNode<CollisionShape2D>("BodyCollider");
            var capsule = (CapsuleShape2D)bodyCollider.Shape;
            var floorCollider = floor.GetNode<CollisionShape2D>("Collision");
            var floorRectangle = (RectangleShape2D)floorCollider.Shape;
            TestAssert.True(!floorCollider.Disabled && Mathf.IsZeroApprox(floorCollider.GlobalRotation) &&
                floorCollider.GlobalScale.IsEqualApprox(Vector2.One) && bodyCollider.Position.IsZeroApprox() &&
                Mathf.IsZeroApprox(bodyCollider.GlobalRotation) && bodyCollider.GlobalScale.IsEqualApprox(Vector2.One),
                "support geometry uses live enabled unrotated unit-scale colliders and a centered actual capsule");
            Rect2 floorBounds = new(floorCollider.GlobalPosition - floorRectangle.Size * .5f, floorRectangle.Size);
            TestAssert.True(floorBounds.Position.IsEqualApprox(new Vector2(1400, 1076)) &&
                floorBounds.Size.IsEqualApprox(new Vector2(700, 24)) &&
                Mathf.IsEqualApprox(capsule.Height, 72) && Mathf.IsEqualApprox(capsule.Radius, 20) &&
                player.GlobalPosition.X - capsule.Radius > floorBounds.Position.X &&
                player.GlobalPosition.X + capsule.Radius < floorBounds.End.X &&
                Mathf.Abs(player.GlobalPosition.Y + capsule.Height * .5f - floorBounds.Position.Y) < .5f,
                "complete saved 72x40 capsule lies within actual LowerCatch bounds and touches its top1076");
            TestAssert.Equal(PrologueObjective.LearnAttackMode, completion.CurrentObjective, "real first attack lesson");
            if (presentation) await KeyboardActionRuntimeTests.Frames(tree, input, 45);
            KeyboardActionRuntimeTests.KeyEvent(Key.J, true);
            await KeyboardActionRuntimeTests.Frames(tree, input, 3);
            KeyboardActionRuntimeTests.KeyEvent(Key.J, false);
            await KeyboardActionRuntimeTests.Frames(tree, input, 30);
            TestAssert.True(attacks == 1 && completion.CurrentObjective == PrologueObjective.LearnGrappleMode,
                "accepted physical J advances the attack lesson, not a Q/E mode event");
            KeyboardActionRuntimeTests.KeyEvent(Key.W, true);
            await KeyboardActionRuntimeTests.Frames(tree, input, 3);
            AssertUpTarget(player, claw, target, 880);
            if (presentation) await KeyboardActionRuntimeTests.Frames(tree, input, 45);
            await Attach(tree, input, claw, target);
            await KeyboardActionRuntimeTests.Frames(tree, input, presentation ? 45 : 3);
            TestAssert.Equal(PrologueObjective.ReachBasin, completion.CurrentObjective,
                "real live-target K attachment naturally completes grapple teaching");
            KeyboardActionRuntimeTests.KeyEvent(Key.K, true);
            await KeyboardActionRuntimeTests.Frames(tree, input, 3);
            KeyboardActionRuntimeTests.KeyEvent(Key.K, false);
            await GroundedIdle(tree, input, player, claw, floor);
            guidance.SetLanguage(GameLanguage.English);
            await Settle(tree);
            var visual = level.GetNode<WorldTargetMarkerVisual>("Hud/Guidance/VisualRoot");
            Vector2 screen = player.GetGlobalTransformWithCanvas() * player.ToLocal(new Vector2(player.GlobalPosition.X, 880));
            TestAssert.True(player.GetViewportRect().Grow(-48).HasPoint(screen), "actual V2 underside is inside prompt viewport inset");
            bool offered = DescentClawGuidance.TryResolve(sector, player, out Vector2 point);
            TestAssert.True(offered && Mathf.Abs(point.Y - 880) < .5f &&
                guidance.CurrentInstruction.Targets.Count == 1 &&
                guidance.CurrentInstruction.Targets[0] == PrologueGuidanceTargetKey.DescentClawOpportunity &&
                visual.HasVisiblePrompt && visual.RenderedTargetCount == 1,
                "DESCENT_V2_KEYBOARD_OPPORTUNITY_RED: supported current V2 ceiling must automatically offer real W/K opportunity");
            AssertOpportunity(level, "saved V2 local opportunity");
            GD.Print($"DESCENT_V2_OPPORTUNITY_OK player={player.GlobalPosition} point={point} screen={screen} " +
                $"same_player={playerId} prior_real_attachment=true first_shelf=false");
            if (presentation) await KeyboardActionRuntimeTests.Frames(tree, input, 60);
            else
            {
                await Boundaries(tree, level, input, claw, target);
                await HeldShield(tree, level, input);
            }
            float beforeY = player.GlobalPosition.Y;
            await Attach(tree, input, claw, target);
            for (int tick = 0; tick < 60 && beforeY - player.GlobalPosition.Y < 24; tick++)
                await KeyboardActionRuntimeTests.Frames(tree, input, 1);
            TestAssert.True(beforeY - player.GlobalPosition.Y >= 24 && IsAttached(claw.CurrentFrame.State),
                "real K produces native upward pull on saved terrain, no injected body or claw state");
            await Settle(tree);
            AssertCard(level, PrologueGuidanceTargetKey.ClawAttachment, "actual attachment");
            string attachedText = guidance.CurrentInstruction.StepText;
            TestAssert.True(attachedText.Contains("auto-pull", StringComparison.OrdinalIgnoreCase) &&
                attachedText.Contains("A/D") && attachedText.Contains("SPACE", StringComparison.OrdinalIgnoreCase) &&
                attachedText.Contains("K AGAIN: CANCEL") && attachedText.Contains("J/L: END CLAW"),
                "attachment retains auto-pull, optional swing, anytime Space, K cancel and J/L end meanings");
            if (presentation) await KeyboardActionRuntimeTests.Frames(tree, input, 60);
            var states = new List<MechanicalClawState>();
            void Released(MechanicalClawFrame frame, GrappleSurface2D _) => states.Add(frame.State);
            var motor = player.GetNode<PlayerMotor>("Motor");
            int jumps = motor.AcceptedJumpCount;
            claw.ClawFrameChanged += Released;
            try
            {
                KeyboardActionRuntimeTests.KeyEvent(Key.Space, true);
                await KeyboardActionRuntimeTests.Frames(tree, input, 2);
                KeyboardActionRuntimeTests.KeyEvent(Key.Space, false);
                await KeyboardActionRuntimeTests.Frames(tree, input, 1);
            }
            finally { claw.ClawFrameChanged -= Released; }
            int boost = states.IndexOf(MechanicalClawState.Boost);
            TestAssert.True(boost >= 0 && states.IndexOf(MechanicalClawState.Released) == boost + 1 &&
                motor.AcceptedJumpCount == jumps,
                "attached physical Space publishes Boost then Released, never an extra ordinary jump");
            await GroundedIdle(tree, input, player, claw, floor);
            await Settle(tree);
            TestAssert.True(player.GetInstanceId() == playerId && requests == 0 && recoveries == 0 &&
                player.GetNode<HealthComponent>("Health").CurrentHealth == 5 &&
                completion.CurrentObjective == PrologueObjective.ReachBasin,
                "same default-health player naturally lands without recovery or objective injection");
            if (presentation) await KeyboardActionRuntimeTests.Frames(tree, input, 45);
            GD.Print($"DESCENT_CLAW_GUIDANCE_RUNTIME_OK layout=2 keyboard=true native_pull_release=true " +
                $"presentation={presentation} recoveries={requests}/{recoveries} hp=5");
        }
        finally
        {
            foreach (Key key in new[] { Key.J, Key.K, Key.W, Key.L, Key.Space })
                KeyboardActionRuntimeTests.KeyEvent(key, false);
            combat.AttackStarted -= Attacked;
            level.RecoveryCompleted -= Recovered;
            level.RecoveryRequested -= Requested;
            await TestSceneTeardown.FreeSceneAfterAudioDrain(tree, level);
        }
    }

    private static async Task Boundaries(SceneTree tree, PrologueLevelController level, PlayerInputReader input,
        MechanicalClawController claw, GrappleSurface2D target)
    {
        var guidance = level.GetNode<PrologueGuidanceController>("Guidance");
        foreach (GameLanguage language in new[] { GameLanguage.ChineseSimplified, GameLanguage.English })
        {
            guidance.SetLanguage(language);
            await Settle(tree);
            AssertOpportunity(level, "language " + language);
            GrappleSurfaceMaterial originalMaterial = target.Material;
            target.Material = GrappleSurfaceMaterial.Ice;
            try
            {
                await Settle(tree);
                TestAssert.True(claw.KeyboardPreview.Surface != target, "actual W preview rejects Ice");
                AssertFallback(level, "Ice surface");
            }
            finally { target.Material = originalMaterial; await Settle(tree); }
        }
        target.Visible = false;
        try
        {
            await Settle(tree);
            AssertAxisTarget(level.Player, target, 880);
            TestAssert.True(!target.IsVisibleInTree(), "hidden negative case retains its real physical ceiling");
            AssertFallback(level, "hidden target");
        }
        finally { target.Visible = true; await Settle(tree); }
        var body = target.GetParent<StaticBody2D>();
        Vector2 original = body.Position;
        body.Position = original + Vector2.Down * 120;
        try
        {
            await Settle(tree);
            AssertUpTarget(level.Player, claw, target, 1000);
            CapsuleShape2D capsule = (CapsuleShape2D)level.Player.GetNode<CollisionShape2D>("BodyCollider").Shape;
            MechanicalClawSettings settings = MechanicalClawSettings.Default;
            Vector2 dock = MechanicalClawArrivalGeometry.DockPoint(claw.KeyboardPreview.Snapshot.WorldAnchor,
                Vector2.Down, capsule, settings.MinimumRopeLength, settings.ArrivalMargin);
            TestAssert.True(Mathf.Abs(dock.Y - 1048) < .5f && dock.Y > level.Player.GlobalPosition.Y &&
                Mathf.Abs(level.Player.GlobalPosition.Y - capsule.Height * .5f - 1000 - 4) < .5f,
                "hittable low roof has four pixels head clearance but its actual dock1048 is below player1040");
            Vector2 screen = level.Player.GetGlobalTransformWithCanvas() *
                level.Player.ToLocal(new Vector2(level.Player.GlobalPosition.X, 1000));
            TestAssert.True(level.Player.GetViewportRect().Grow(-48).HasPoint(screen),
                "unusable low roof remains visible, not merely offscreen");
            AssertFallback(level, "native hittable roof with no upward dock space");
        }
        finally { body.Position = original; await Settle(tree); }

        // A real body-side obstruction, clear of the current capsule and the W axis.
        var blocker = new StaticBody2D { Name = "GuidanceBodyFlankBlocker", Position = new Vector2(1618, 970),
            CollisionLayer = CombatCollisionLayers.World };
        blocker.AddChild(new CollisionShape2D { Shape = new RectangleShape2D { Size = new Vector2(8, 8) } });
        level.AddChild(blocker);
        try
        {
            await Settle(tree);
            AssertUpTarget(level.Player, claw, target, 880);
            var capsule = (CapsuleShape2D)level.Player.GetNode<CollisionShape2D>("BodyCollider").Shape;
            MechanicalClawSettings settings = MechanicalClawSettings.Default;
            Vector2 dock = MechanicalClawArrivalGeometry.DockPoint(claw.KeyboardPreview.Snapshot.WorldAnchor,
                Vector2.Down, capsule, settings.MinimumRopeLength, settings.ArrivalMargin);
            Vector2 displacement = dock - level.Player.GlobalPosition;
            TestAssert.True(Mathf.Abs(displacement.X) < .5f && Mathf.Abs(displacement.Y + 112) < .5f,
                "full current-capsule dock sweep is independently112 pixels upward");
            using var collision = new KinematicCollision2D();
            TestAssert.True(level.Player.TestMove(level.Player.GlobalTransform, displacement, collision,
                level.Player.SafeMargin, false) && collision.GetCollider() == blocker,
                "native actual-body query hits flank blocker despite unobstructed W target ray");
            AssertFallback(level, "body-side blocker");
        }
        finally { blocker.QueueFree(); await Settle(tree); }
        AssertOpportunity(level, "native body path restored");
        KeyboardActionRuntimeTests.KeyEvent(Key.J, true);
        await KeyboardActionRuntimeTests.Frames(tree, input, 3);
        KeyboardActionRuntimeTests.KeyEvent(Key.J, false);
        await Settle(tree);
        TestAssert.True(level.Player.GetNode<PlayerCombat>("Combat").IsAttacking, "real second J is active");
        TestAssert.True(level.Player.GetNode<PlayerActionModeController>("ActionModes").CanBeginGrapple,
            "current real action authority permits K during J; guidance must not invent a new attack lock");
        AssertOpportunity(level, "K remains available during real J");
        await KeyboardActionRuntimeTests.Frames(tree, input, 30);
        AssertOpportunity(level, "attack finished");
        guidance.SetGuidanceEnabled(false);
        try
        {
            await Settle(tree);
            var visual = level.GetNode<WorldTargetMarkerVisual>("Hud/Guidance/VisualRoot");
            TestAssert.True(!guidance.CurrentInstruction.Visible && !visual.HasVisiblePrompt &&
                visual.RenderedTargetCount == 0, "disabled guidance clears actual card and marker");
        }
        finally { guidance.SetGuidanceEnabled(true); await Settle(tree); }
        AssertOpportunity(level, "guidance restored");
    }

    private static async Task HeldShield(SceneTree tree, PrologueLevelController level, PlayerInputReader input)
    {
        var shield = level.Player.GetNode<PlayerShieldController>("Shield");
        var energy = level.Player.GetNode<PlayerEnergyController>("Energy");
        KeyboardActionRuntimeTests.KeyEvent(Key.L, true);
        try
        {
            await Settle(tree);
            TestAssert.True(input.Current.ShieldHeld && shield.IsActive, "real L starts sustained guard");
            AssertFallback(level, "held active L");
            for (int tick = 0; tick < 300 && !energy.IsOverheated; tick++)
                await KeyboardActionRuntimeTests.Frames(tree, input, 1);
            await Settle(tree);
            TestAssert.True(energy.IsOverheated && !shield.IsActive && input.Current.ShieldHeld,
                "natural overheat keeps real L held while shield becomes inactive");
            AssertFallback(level, "inactive overheated shield with L held");
        }
        finally { KeyboardActionRuntimeTests.KeyEvent(Key.L, false); await Settle(tree); }
        AssertOpportunity(level, "L released after natural overheat");
        KeyboardActionRuntimeTests.KeyEvent(Key.L, true);
        try
        {
            await Settle(tree);
            TestAssert.True(input.Current.ShieldHeld, "second physical L is independently sampled without resetting energy");
            AssertFallback(level, "second held L");
        }
        finally { KeyboardActionRuntimeTests.KeyEvent(Key.L, false); await Settle(tree); }
        AssertOpportunity(level, "second L release");
    }

    private static void AssertUpTarget(CharacterBody2D player, MechanicalClawController claw,
        GrappleSurface2D target, float expectedY)
    {
        var preview = claw.KeyboardPreview;
        TestAssert.True(preview.Surface == target && preview.Snapshot.Exists && preview.Snapshot.HasLineOfSight &&
            preview.Snapshot.SurfaceNormal.IsEqualApprox(Vector2.Down) &&
            Mathf.Abs(preview.Snapshot.WorldAnchor.Y - expectedY) < .5f,
            "actual W keyboard preview is the saved underside at independently derived Y=" + expectedY);
        AssertAxisTarget(player, target, expectedY);
    }

    private static void AssertAxisTarget(CharacterBody2D player, GrappleSurface2D target, float expectedY)
    {
        using var query = PhysicsRayQueryParameters2D.Create(player.GlobalPosition,
            new Vector2(player.GlobalPosition.X, expectedY - 1));
        query.CollisionMask = CombatCollisionLayers.World;
        query.CollideWithAreas = false;
        using var hit = player.GetWorld2D().DirectSpaceState.IntersectRay(query);
        TestAssert.True(hit.ContainsKey("collider") && hit["collider"].AsGodotObject() == target.GetParent() &&
            Mathf.Abs(hit["position"].AsVector2().Y - expectedY) < .5f,
            "independent native axis first hit agrees with keyboard underside");
    }

    private static async Task Attach(SceneTree tree, PlayerInputReader input, MechanicalClawController claw,
        GrappleSurface2D target)
    {
        KeyboardActionRuntimeTests.KeyEvent(Key.K, true);
        for (int tick = 0; tick < 60 && !IsAttached(claw.CurrentFrame.State); tick++)
            await KeyboardActionRuntimeTests.Frames(tree, input, 1);
        KeyboardActionRuntimeTests.KeyEvent(Key.K, false);
        TestAssert.True(IsAttached(claw.CurrentFrame.State) && claw.CurrentTarget == target &&
            claw.CurrentFrame.LockedTargetRuntimeId == target.GetInstanceId(),
            "physical K attaches to the exact shown live terrain target");
        await KeyboardActionRuntimeTests.Frames(tree, input, 1);
    }

    private static async Task GroundedIdle(SceneTree tree, PlayerInputReader input, CharacterBody2D player,
        MechanicalClawController claw, StaticBody2D floor)
    {
        int stable = 0;
        for (int tick = 0; tick < 180 && stable < 6; tick++)
        {
            await KeyboardActionRuntimeTests.Frames(tree, input, 1);
            stable = Supported(player, floor) && claw.CurrentFrame.State == MechanicalClawState.Idle ? stable + 1 : 0;
        }
        TestAssert.Equal(6, stable, "natural six-sample supported Idle on actual LowerCatch");
    }

    private static bool Supported(CharacterBody2D player, StaticBody2D floor)
    {
        for (int index = 0; player.IsOnFloor() && index < player.GetSlideCollisionCount(); index++)
        {
            var contact = player.GetSlideCollision(index);
            if (contact.GetCollider() == floor && contact.GetNormal().Y < -.7f) return true;
        }
        return false;
    }

    private static bool IsAttached(MechanicalClawState state) =>
        state is MechanicalClawState.Latched or MechanicalClawState.Reel or MechanicalClawState.Swing;

    private static void AssertOpportunity(PrologueLevelController level, string stage)
    {
        AssertCard(level, PrologueGuidanceTargetKey.DescentClawOpportunity, stage);
        var guidance = level.GetNode<PrologueGuidanceController>("Guidance");
        string text = guidance.CurrentInstruction.StepText;
        bool meanings = guidance.Language == GameLanguage.English
            ? text.Contains("ROUGH CEILING") && text.Contains("W: AIM UP") && text.Contains("DIAMOND THEN K") &&
                text.Contains("HIT: auto-pull") && text.Contains("A/D: DESCEND")
            : text.Contains("粗糙顶面") && text.Contains("W 瞄准") && text.Contains("K 发爪") &&
                text.Contains("命中：自动牵引") && text.Contains("A/D：继续下降");
        TestAssert.True(meanings, "complete rough/W/diamond/K/auto-pull/optional-descent meanings: " + stage);
    }

    private static void AssertFallback(PrologueLevelController level, string stage)
    {
        AssertCard(level, PrologueGuidanceTargetKey.DescentIce, stage);
        string text = level.GetNode<PrologueGuidanceController>("Guidance").CurrentInstruction.StepText;
        TestAssert.True(text.Contains("冰") && text.Contains("不可抓") && text.Contains("A/D") ||
            text.Contains("ICE", StringComparison.OrdinalIgnoreCase) &&
            text.Contains("NO GRIP", StringComparison.OrdinalIgnoreCase) && text.Contains("A/D: DESCEND"),
            "ineligible opportunity restores the actual Ice cannot-grab warning: " + stage);
    }

    private static void AssertCard(PrologueLevelController level, PrologueGuidanceTargetKey expected, string stage)
    {
        var instruction = level.GetNode<PrologueGuidanceController>("Guidance").CurrentInstruction;
        var visual = level.GetNode<WorldTargetMarkerVisual>("Hud/Guidance/VisualRoot");
        GD.Print($"DESCENT_V2_CARD stage={stage} target={string.Join(",", instruction.Targets)} " +
            $"rendered={visual.RenderedTargetCount} text={instruction.StepText}");
        TestAssert.True(instruction.Visible && visual.HasVisiblePrompt && visual.RenderedTextContained &&
            instruction.Targets.Count == 1 && instruction.Targets[0] == expected &&
            visual.RenderedTargetCount == 1 && visual.RenderedStepText == instruction.StepText,
            "automatic single rendered complete card matches " + expected + ": " + stage);
    }

    private static async Task Settle(SceneTree tree)
    {
        for (int frame = 0; frame < 2; frame++) await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
        for (int frame = 0; frame < 3; frame++) await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
    }
}
