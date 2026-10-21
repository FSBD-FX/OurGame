using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Knight.Abilities;
using Knight.Player;
using Knight.Combat;
using Knight.Enemies.Laborer;
using Knight.Prologue.Sample;
using Knight.Localization;
using Knight.Prologue;
using Knight.Prologue.Guidance;
using Knight.Prologue.Seamless;
using Knight.Settings;
using Knight.Shared;
using Knight.Ui;

namespace Knight.Tests.Integration;

public static class KeyboardTeachingRuntimeTests
{
    public static async Task RunThrusterTeaching(SceneTree tree)
    {
        var level = GD.Load<PackedScene>("res://scenes/prologue/PrologueLevel.tscn")
            .Instantiate<PrologueLevelController>();
        IsolatedPrologueFixture.Configure(level, "thruster-first-encounter");
        var flow = level.GetNode<PrologueGameFlowController>("GameFlow");
        flow.HeadlessAutoStart = false;
        ThrusterPlayerRuntimeTests.Release(); tree.Root.AddChild(level);
        try
        {
            await Frames(tree, 4); flow.StartNew();
            var input = level.Player.GetNode<PlayerInputReader>("Input");
            await KeyboardActionRuntimeTests.Frames(tree, input, 8);
            var completion = level.GetNode<PrologueCompletionController>("Completion");
            var guidance = level.GetNode<PrologueGuidanceController>("Guidance");
            var visual = level.GetNode<WorldTargetMarkerVisual>("Hud/Guidance/VisualRoot");
            TestAssert.True(flow.State == PrologueGameFlowState.Playing && level.Player.IsOnFloor() &&
                completion.CurrentObjective == PrologueObjective.LearnAttackMode && guidance.SameSectorFailureCount == 0,
                "real supported first spawn has not attacked, failed, or skipped the initial lesson");
            foreach (GameLanguage language in Enum.GetValues<GameLanguage>())
            {
                guidance.SetLanguage(language); guidance.RefreshNow(); await Frames(tree, 3);
                string text = guidance.CurrentInstruction.StepText;
                TestAssert.True(text.Contains("SHIFT", StringComparison.OrdinalIgnoreCase) &&
                    text.Contains(language == GameLanguage.English ? "SHORT JET" : "短喷") &&
                    text.Contains("A") && text.Contains("D") && visual.IsVisibleInTree() &&
                    visual.HasVisiblePrompt && visual.RenderedTextContained && visual.RenderedStepText == text &&
                    visual.GetNode<Label>("Text").Text.Contains(text),
                    "actual initial mounted visible card teaches short jet/braking in both languages without clipping");
            }
            GD.Print("THRUSTER_TEACHING_UI_OK real_initial_spawn=true no_failure_required=true controls_pause_localized=true visible_card_contained=true headless_layout_only=true human=false");
        }
        finally
        {
            ThrusterPlayerRuntimeTests.Release(); tree.Paused = false;
            var owners = CaptureTeachingOwners("entry", level);
            level.Free(); await Frames(tree, 2);
            ReportTeachingOwners("entry-after-existing-teardown", owners);
        }
    }

    // Opt-in diagnosis only. Retain scalar IDs, never Node/Resource/query wrappers
    // across teardown or await. No extra load, instantiate, frames, GC or disposal.
    private static (string Label, bool Present, ulong Id)[] CaptureTeachingOwners(
        string prefix, Node main, Node ui = null, Node viewport = null, Node pause = null)
    {
        if (Array.IndexOf(OS.GetCmdlineUserArgs(), "--trace-teaching-owners") < 0)
            return Array.Empty<(string, bool, ulong)>();
        var result = new List<(string, bool, ulong)>();
        void Add(string label, GodotObject value) => result.Add((prefix + "." + label,
            value != null, value?.GetInstanceId() ?? 0));
        object Field(object value, string name) => value?.GetType().GetField(name,
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(value);
        Add("main", main); Add("ui", ui); Add("viewport", viewport); Add("pause", pause);
        Node player = main?.GetNodeOrNull("PlayerRoot/Player");
        Node motor = player?.GetNodeOrNull("Motor");
        Add("player", player); Add("motor", motor);
        Add("flow", main?.GetNodeOrNull("GameFlow"));
        Add("completion", main?.GetNodeOrNull("Completion"));
        Add("guidance", main?.GetNodeOrNull("Guidance"));
        Add("guide_visual", main?.GetNodeOrNull("Hud/Guidance/VisualRoot"));
        Add("equip_probe", Field(motor, "_equipSupportProbe") as GodotObject);
        object sampler = Field(motor, "_landingSampler");
        foreach (string name in new[] { "_overlap", "_predicted", "_motionOnly", "_supportParameters", "_supportResult" })
            Add("sampler" + name, Field(sampler, name) as GodotObject);
        var ids = result.ToArray(); ReportTeachingOwners(prefix + "-before-teardown", ids);
        return ids;
    }

    private static void ReportTeachingOwners(string stage, (string Label, bool Present, ulong Id)[] owners)
    {
        foreach (var owner in owners)
            GD.Print($"TEACHING_OWNER_ID stage={stage} label={owner.Label} present={owner.Present} " +
                $"id={owner.Id} valid={owner.Present && GodotObject.IsInstanceIdValid(owner.Id)} diagnostic_only=true");
    }

    // Bounded spatial arrangements in the original saved main. Teleporting to a
    // measured support is fixture setup, never evidence of a naturally walked route.
    public static async Task RunAscentTransferGuidance(SceneTree tree)
    {
        var level = GD.Load<PackedScene>("res://scenes/prologue/PrologueLevel.tscn")
            .Instantiate<PrologueLevelController>();
        var paths = IsolatedPrologueFixture.Configure(level, "ascent-transfer-guidance");
        var flow = level.GetNode<PrologueGameFlowController>("GameFlow");
        flow.HeadlessAutoStart = false;
        flow.AutomationAutoStart = false;
        int recoveryRequests = 0, recoveries = 0;
        void RecoveryRequested(string reason)
        {
            recoveryRequests++;
            GD.Print($"ASCENT_TRANSFER_FIXTURE_RECOVERY_REQUEST reason={reason} requests={recoveryRequests} " +
                $"position={level.Player?.GlobalPosition} sector={level.CurrentSector} flow={flow.State}");
        }
        void RecoveryCompleted(string reason)
        {
            recoveries++;
            GD.Print($"ASCENT_TRANSFER_FIXTURE_RECOVERY_COMPLETE reason={reason} recoveries={recoveries} " +
                $"position={level.Player?.GlobalPosition} sector={level.CurrentSector} flow={flow.State}");
        }
        level.RecoveryRequested += RecoveryRequested;
        level.RecoveryCompleted += RecoveryCompleted;
        try
        {
            tree.Root.AddChild(level);
            await Frames(tree, 4);
            var player = level.Player;
            var input = player.GetNode<PlayerInputReader>("Input");
            var body = player.GetNode<CollisionShape2D>("BodyCollider");
            var motor = player.GetNode<PlayerMotor>("Motor");
            var health = player.GetNode<HealthComponent>("Health");
            var completion = level.GetNode<PrologueCompletionController>("Completion");
            var guidance = level.GetNode<PrologueGuidanceController>("Guidance");
            var visual = level.GetNode<WorldTargetMarkerVisual>("Hud/Guidance/VisualRoot");
            var world = level.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors");
            var pause = level.GetNode<ProloguePauseMenu>("PauseMenu");
            PrologueSectorDefinition definition = null;
            foreach (PrologueSectorDefinition candidate in world.Definitions)
                if (candidate.Id == PrologueSectorId.CanyonAscent) definition = candidate;
            TestAssert.True(definition != null && world.LayoutVersion == CanyonWorldLayout.CurrentLayoutVersion &&
                body.Shape is CapsuleShape2D && !body.Disabled,
                "AscentTransferRuntime fixture uses saved layout2 and the original enabled player capsule");
            var capsule = (CapsuleShape2D)body.Shape;
            float radius = capsule.Radius * Mathf.Abs(body.GlobalTransform.X.X);
            float halfHeight = capsule.Height * Mathf.Abs(body.GlobalTransform.Y.Y) / 2f;
            Vector2 bodyOffset = body.GlobalPosition - player.GlobalPosition;
            TestAssert.True(radius > 0f && halfHeight >= radius &&
                Mathf.Abs(body.GlobalTransform.X.Y) < .001f && Mathf.Abs(body.GlobalTransform.Y.X) < .001f,
                "AscentTransferRuntime arrangements require the actual upright capsule");

            void PlacementDiagnostic(string phase, string support, Vector2 target)
            {
                GD.Print($"ASCENT_TRANSFER_FIXTURE_PLACEMENT phase={phase} support={support} target={target} " +
                    $"actual={player.GlobalPosition} velocity={player.Velocity} hp={health.CurrentHealth} " +
                    $"sector={level.CurrentSector} flow={flow.State} known_bounds={definition.WorldBounds} " +
                    $"target_in_bounds={definition.WorldBounds.HasPoint(target)} " +
                    $"requests={recoveryRequests} recoveries={recoveries} " +
                    $"last_fall_height={motor.LastFallHeight:R} last_fall_damage={motor.LastFallDamage} " +
                    $"fall_damage_applied={motor.LastFallDamageApplied} mounted={string.Join(",", world.MountedSectorIds)}");
            }

            async Task Tick()
            {
                await KeyboardActionRuntimeTests.Frames(tree, input, 1);
                // Let the automatic guidance _Process and its real UI layout run.
                await Frames(tree, 2);
            }
            Rect2 Rectangle(PhysicsBody2D support)
            {
                var shape = support.GetNode<CollisionShape2D>("Collision");
                TestAssert.True(!shape.Disabled && shape.Shape is RectangleShape2D &&
                    (support.CollisionLayer & player.CollisionMask) != 0 &&
                    shape.GlobalTransform.X.X > 0f && shape.GlobalTransform.Y.Y > 0f &&
                    Mathf.Abs(shape.GlobalTransform.X.Y) < .001f && Mathf.Abs(shape.GlobalTransform.Y.X) < .001f,
                    "AscentTransferRuntime reads original enabled upright world support " + support.Name);
                var rectangle = (RectangleShape2D)shape.Shape;
                Vector2 start = shape.ToGlobal(-rectangle.Size / 2f);
                Vector2 end = shape.ToGlobal(rectangle.Size / 2f);
                return new Rect2(start, end - start);
            }
            bool Contact(PhysicsBody2D support)
            {
                if (!player.IsOnFloor()) return false;
                for (int index = 0; index < player.GetSlideCollisionCount(); index++)
                {
                    using KinematicCollision2D contact = player.GetSlideCollision(index);
                    if (contact.GetCollider() == support && contact.GetNormal().Y < -.7f) return true;
                }
                return false;
            }
            bool FullSupport(PhysicsBody2D support)
            {
                Rect2 floor = Rectangle(support);
                return Contact(support) && body.GlobalPosition.X - radius >= floor.Position.X &&
                    body.GlobalPosition.X + radius <= floor.End.X;
            }
            Vector2 Spawn(PhysicsBody2D support, float? centerX = null)
            {
                Rect2 floor = Rectangle(support);
                // Grip1's right interior is clear of its separate overhead post.
                float x = centerX ?? (support.Name == new StringName("AscentGrip1")
                    ? floor.End.X - radius - 10f : floor.GetCenter().X);
                return new Vector2(x - bodyOffset.X, floor.Position.Y - halfHeight - bodyOffset.Y - 4f);
            }
            async Task<PrologueSector> LoadSector(string firstSupport)
            {
                // Read an unmounted copy only to derive an in-bounds first placement.
                // Normal streaming, not a reflected/private mount call, owns the live sector.
                var saved = definition.Scene.Instantiate<PrologueSector>();
                Vector2 placement;
                try
                {
                    var support = saved.GetNode<StaticBody2D>("Gameplay/Collision/" + firstSupport);
                    Vector2 localPlacement = Spawn(support);
                    placement = definition.WorldOffset + localPlacement;
                    GD.Print($"ASCENT_TRANSFER_FIXTURE_SAVED_GEOMETRY support={firstSupport} " +
                        $"saved_root={saved.Position} saved_support_local={support.Position} " +
                        $"saved_shape_global={support.GetNode<CollisionShape2D>("Collision").GlobalPosition} " +
                        $"local_placement={localPlacement} world_offset={definition.WorldOffset}");
                }
                finally { saved.Free(); }
                PlacementDiagnostic("load-before", firstSupport, placement);
                player.GlobalPosition = placement;
                player.Velocity = Vector2.Zero;
                // Match the production world's explicit-relocation boundary: the
                // artificial teleport distance is not a naturally fallen height.
                motor.ResetFallTracking();
                PlacementDiagnostic("load-after-placement", firstSupport, placement);
                for (int tick = 0; tick < 60; tick++)
                {
                    await Tick();
                    if (tick is 0 or 2 or 59) PlacementDiagnostic("load-tick-" + tick, firstSupport, placement);
                    if (level.CurrentSector == PrologueSectorId.CanyonAscent &&
                        world.TryGetMountedSector(PrologueSectorId.CanyonAscent, out PrologueSector mounted))
                    {
                        PlacementDiagnostic("load-mounted", firstSupport, placement);
                        return mounted;
                    }
                }
                throw new InvalidOperationException("AscentTransferRuntime could not stream the original ascent sector");
            }
            async Task Arrange(PhysicsBody2D support, bool expectedFull = true, float? centerX = null)
            {
                await Frames(tree, 1);
                Vector2 placement = Spawn(support, centerX);
                PlacementDiagnostic("arrange-before", support.Name.ToString(), placement);
                player.GlobalPosition = placement;
                player.Velocity = Vector2.Zero;
                motor.ResetFallTracking();
                PlacementDiagnostic("arrange-after-placement", support.Name.ToString(), placement);
                int stable = 0;
                for (int tick = 0; tick < 60 && stable < 3; tick++)
                {
                    await Tick();
                    stable = Contact(support) && FullSupport(support) == expectedFull ? stable + 1 : 0;
                }
                PlacementDiagnostic(stable == 3 ? "arrange-supported" : "arrange-failed",
                    support.Name.ToString(), placement);
                TestAssert.True(stable == 3 &&
                    player.GetNode<MechanicalClawController>("MechanicalClaw").CurrentFrame.State == MechanicalClawState.Idle,
                    $"AscentTransferRuntime arrangement settles on exact {support.Name}, full={expectedFull}, " +
                    $"player={player.GlobalPosition}, floor={player.IsOnFloor()}");
            }
            string LiveGlyph(StringName action)
            {
                foreach (InputEvent binding in InputMap.ActionGetEvents(action))
                    if (binding is InputEventKey key)
                        return OS.GetKeycodeString(key.PhysicalKeycode != Key.None
                            ? key.PhysicalKeycode : key.Keycode).ToUpperInvariant();
                throw new InvalidOperationException("AscentTransferRuntime requires live keyboard binding " + action);
            }
            bool TransferText(string text) =>
                text.Contains(LiveGlyph(InputActions.Jump), StringComparison.Ordinal) &&
                text.Contains(LiveGlyph(InputActions.Claw), StringComparison.Ordinal) &&
                text.Contains(LiveGlyph(InputActions.MoveUp) + "+" + LiveGlyph(InputActions.MoveRight), StringComparison.Ordinal) &&
                (text.Contains("up-right", StringComparison.OrdinalIgnoreCase) || text.Contains("右上", StringComparison.Ordinal)) &&
                text.IndexOf(LiveGlyph(InputActions.Jump), StringComparison.Ordinal) <
                    text.IndexOf(LiveGlyph(InputActions.Claw), StringComparison.Ordinal);
            void Prompt(string context)
            {
                PrologueGuidanceInstruction instruction = guidance.CurrentInstruction;
                TestAssert.True(instruction.Visible && TransferText(instruction.StepText),
                    context + ": actual=" + instruction.StepText);
                Label label = visual.GetNode<Label>("Text");
                TestAssert.True(instruction.Targets.Count == 0 && visual.RenderedTargetCount == 0 &&
                    visual.HasVisiblePrompt && visual.RenderedTextContained && visual.IsVisibleInTree() &&
                    visual.RenderedStepText == instruction.StepText && label.IsVisibleInTree() &&
                    label.Text.Contains(instruction.StepText, StringComparison.Ordinal) &&
                    visual.PromptRect.Grow(.5f).Encloses(label.GetRect()) &&
                    label.GetMinimumSize().Y <= label.Size.Y + .5f,
                    context + ": full automatic formal card fits its actual label without a route marker");
                TestAssert.Equal(PrologueObjective.EnterTemple, completion.CurrentObjective,
                    context + ": local help never consumes or replaces the genuine ascent objective");
            }
            void NoPrompt(string context)
            {
                TestAssert.True(!TransferText(guidance.CurrentInstruction.StepText) &&
                    !TransferText(visual.RenderedStepText), context + ": no ascent-transfer card or stale rendered copy");
            }
            async Task CaptureCard(GameLanguage language)
            {
                if (DisplayServer.GetName() == "headless") return;
                await tree.ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                Prompt("AscentTransferRuntime drawn " + language + " card");
                using Image image = tree.Root.GetTexture().GetImage();
                TestAssert.True(!image.IsEmpty() && image.GetWidth() == tree.Root.GetTexture().GetWidth() &&
                    image.GetHeight() == tree.Root.GetTexture().GetHeight(),
                    "AscentTransferRuntime retains actual unscaled display-backed viewport pixels");
                string path = System.IO.Path.Combine(paths.CaseRoot, "ascent-help-" + language + ".png");
                TestAssert.Equal(Error.Ok, image.SavePng(path), "AscentTransferRuntime saves original proof pixels");
                GD.Print($"ASCENT_TRANSFER_CARD_CAPTURE path={path} language={language} " +
                    $"width={image.GetWidth()} height={image.GetHeight()} controlled_fixture=true natural_route=false");
            }
            async Task NewSession()
            {
                if (flow.State != PrologueGameFlowState.Title) flow.ReturnToTitle();
                flow.StartNew();
                await Tick();
                TestAssert.Equal(PrologueGameFlowState.Playing, flow.State,
                    "AscentTransferRuntime uses the real StartNew session lifecycle");
                guidance.SetGuidanceEnabled(true);
                guidance.SetLanguage(GameLanguage.English);
                // Explicit precondition arrangement, not proof of the earlier lesson chain.
                completion.RestoreSession(PrologueObjective.ClimbCanyon);
            }
            StaticBody2D Shelf(PrologueSector sector, string name) =>
                sector.GetNode<StaticBody2D>("Gameplay/Collision/" + name);

            await NewSession();
            PrologueSector ascent = await LoadSector("AscentGrip1");
            StaticBody2D grip1 = Shelf(ascent, "AscentGrip1");
            await Arrange(grip1);
            Prompt("AscentTransferRuntime: full saved first shelf support presents jump then up-right claw help");
            await CaptureCard(GameLanguage.English);
            guidance.SetLanguage(GameLanguage.ChineseSimplified);
            await Tick();
            Prompt("AscentTransferRuntime Chinese formal card");
            await CaptureCard(GameLanguage.ChineseSimplified);
            guidance.SetLanguage(GameLanguage.English);
            await Tick();

            pause.Open();
            await Frames(tree, 2);
            TestAssert.True(tree.Paused && flow.State == PrologueGameFlowState.Paused &&
                !guidance.CurrentInstruction.Visible && !visual.HasVisiblePrompt,
                "AscentTransferPause: an active transfer card is withdrawn while the real pause menu owns the screen");
            guidance.SetGuidanceEnabled(false);
            guidance.SetGuidanceEnabled(true);
            guidance.SetLanguage(GameLanguage.ChineseSimplified);
            await Frames(tree, 2);
            TestAssert.True(!guidance.CurrentInstruction.Visible && !visual.HasVisiblePrompt,
                "paused settings refresh cannot reopen the hidden teaching card");
            guidance.SetLanguage(GameLanguage.English);
            pause.Execute(ProloguePauseCommand.Resume);
            await Tick();
            Prompt("AscentTransferRuntime resuming before success restores local help");

            var rebound = new Dictionary<StringName, Godot.Collections.Array<InputEvent>>();
            foreach (StringName action in new[] { InputActions.Jump, InputActions.Claw,
                         InputActions.MoveUp, InputActions.MoveRight })
                rebound[action] = InputMap.ActionGetEvents(action);
            try
            {
                var bindings = new (StringName Action, Key Key)[]
                { (InputActions.Jump, Key.U), (InputActions.Claw, Key.H),
                    (InputActions.MoveUp, Key.Up), (InputActions.MoveRight, Key.Right) };
                foreach (var binding in bindings)
                {
                    InputMap.ActionEraseEvents(binding.Action);
                    InputMap.ActionAddEvent(binding.Action, new InputEventKey
                        { PhysicalKeycode = binding.Key, Keycode = binding.Key });
                }
                await Tick();
                Prompt("AscentTransferRuntime live remapped glyphs");
            }
            finally
            {
                foreach (var binding in rebound)
                {
                    InputMap.ActionEraseEvents(binding.Key);
                    foreach (InputEvent inputEvent in binding.Value) InputMap.ActionAddEvent(binding.Key, inputEvent);
                }
            }
            await Tick();
            KeyboardActionRuntimeTests.KeyEvent(Key.Space, true);
            try
            {
                for (int tick = 0; tick < 8 && player.IsOnFloor(); tick++) await Tick();
                TestAssert.True(!player.IsOnFloor(), "AscentTransferRuntime real Space leaves the original shelf");
                NoPrompt("AscentTransferRuntime airborne jump");
            }
            finally { KeyboardActionRuntimeTests.KeyEvent(Key.Space, false); }

            await Arrange(grip1, false, Rectangle(grip1).End.X - radius / 2f);
            NoPrompt("AscentTransferRuntime partially supported Grip1 edge");
            await Arrange(Shelf(ascent, "AscentGrip1Grip"), false);
            NoPrompt("AscentTransferRuntime Grip1 narrow post is not its shelf");
            await Arrange(Shelf(ascent, "AscentGrip2Grip"), false);
            NoPrompt("AscentTransferRuntime Grip2 narrow post is not success");
            await Arrange(grip1);
            Prompt("AscentTransferRuntime narrow-post visit does not consume help");
            StaticBody2D grip2 = Shelf(ascent, "AscentGrip2");
            await Arrange(grip2, false, Rectangle(grip2).End.X - radius / 2f);
            NoPrompt("AscentTransferRuntime partial Grip2 edge is not full success");
            await Arrange(grip1);
            Prompt("AscentTransferRuntime partial Grip2 visit does not consume help");

            await NewSession();
            ascent = await LoadSector("LiftEntryFloor");
            var lift = ascent.GetNode<AnimatableBody2D>("Gameplay/Mechanisms/CounterweightLift");
            await Arrange(lift);
            NoPrompt("AscentTransferRuntime original inactive main lift is not Grip1");
            // A direct controlled Grip2 landing represents the main route's arrival state,
            // not a simulated lift ride. Its exact shelf support must retire the lesson.
            grip2 = Shelf(ascent, "AscentGrip2");
            await Arrange(grip2);
            NoPrompt("AscentTransferRuntime direct main-route Grip2 arrival");
            grip1 = Shelf(ascent, "AscentGrip1");
            await Arrange(grip1);
            NoPrompt("AscentTransferRuntime full Grip2 success persists on return to Grip1");
            pause.Open();
            await Frames(tree, 2);
            TestAssert.True(tree.Paused && flow.State == PrologueGameFlowState.Paused,
                "AscentTransferRuntime opens the real pause lifecycle");
            pause.Execute(ProloguePauseCommand.Resume);
            await Tick();
            NoPrompt("AscentTransferRuntime pause/resume preserves success memory");
            // Use the real menu return/recovery chain in this isolated session. It
            // is still a requested checkpoint return, not a natural hazard death.
            int requestsBefore = recoveryRequests, recoveriesBefore = recoveries;
            ulong playerId = player.GetInstanceId();
            pause.Open();
            pause.Execute(ProloguePauseCommand.ReturnToCheckpoint);
            for (int tick = 0; tick < 60 && (recoveries == recoveriesBefore ||
                 flow.State != PrologueGameFlowState.Playing); tick++) await Tick();
            TestAssert.True(recoveryRequests == requestsBefore + 1 && recoveries == recoveriesBefore + 1 &&
                flow.State == PrologueGameFlowState.Playing && level.Player.GetInstanceId() == playerId &&
                level.CurrentSector == PrologueSectorId.CanyonEntrance,
                "AscentTransferRuntime real menu return reaches the initial legal checkpoint with the same actor");
            ascent = await LoadSector("AscentGrip1");
            await Arrange(Shelf(ascent, "AscentGrip1"));
            NoPrompt("AscentTransferRuntime return and re-entering the sector preserve success memory");
            GD.Print("ASCENT_TRANSFER_CHECKPOINT_RETURN_OK real_recovery_chain=true " +
                "controlled_menu_request=true natural_death=false");

            await NewSession();
            ascent = await LoadSector("AscentGrip1");
            grip1 = Shelf(ascent, "AscentGrip1");
            await Arrange(grip1);
            Prompt("AscentTransferRuntime new session resets prior success");
            guidance.SetGuidanceEnabled(false);
            await Tick();
            TestAssert.True(!guidance.CurrentInstruction.Visible && !visual.HasVisiblePrompt,
                "AscentTransferRuntime disabled guidance hides the real card");
            await Arrange(Shelf(ascent, "AscentGrip2"));
            guidance.SetGuidanceEnabled(true);
            await Tick();
            await Arrange(grip1);
            NoPrompt("AscentTransferRuntime success while disabled remains remembered after enabling");
            await NewSession();
            ascent = await LoadSector("AscentGrip1");
            await Arrange(Shelf(ascent, "AscentGrip1"));
            Prompt("AscentTransferRuntime another StartNew resets disabled-session success");
            GD.Print("ASCENT_TRANSFER_GUIDANCE_RUNTIME_OK saved_main=true controlled_arrangements=true " +
                "natural_route=false natural_death=false");
        }
        finally
        {
            KeyboardActionRuntimeTests.KeyEvent(Key.Space, false);
            tree.Paused = false;
            if (GodotObject.IsInstanceValid(level))
            {
                level.RecoveryRequested -= RecoveryRequested;
                level.RecoveryCompleted -= RecoveryCompleted;
                if (level.IsInsideTree()) await TestSceneTeardown.FreeSceneAfterAudioDrain(tree, level);
                else level.Free();
            }
        }
    }

    public static async Task<string> RunProgress(SceneTree tree)
    {
        var level = GD.Load<PackedScene>("res://scenes/prologue/PrologueLevel.tscn")
            .Instantiate<PrologueLevelController>();
        var paths = IsolatedPrologueFixture.Configure(level, "keyboard-teaching");
        var flow = level.GetNode<PrologueGameFlowController>("GameFlow");
        flow.HeadlessAutoStart = false;
        tree.Root.AddChild(level);
        await Frames(tree, 4);
        flow.StartNew();
        var player = level.Player;
        var input = player.GetNode<PlayerInputReader>("Input");
        var completion = level.GetNode<PrologueCompletionController>("Completion");
        var energy = player.GetNode<PlayerEnergyController>("Energy");
        var modes = player.GetNode<PlayerActionModeController>("ActionModes");
        var claw = player.GetNode<MechanicalClawController>("MechanicalClaw");
        int attacks = 0;
        player.GetNode<PlayerCombat>("Combat").AttackStarted += (_, _) => attacks++;
        try
        {
            await KeyboardActionRuntimeTests.Frames(tree, input, 8);
            TestAssert.True(flow.State == PrologueGameFlowState.Playing && player.IsOnFloor(),
                "teaching fixture uses an isolated running level and real supported spawn");
            TestAssert.Equal(PrologueObjective.LearnAttackMode, completion.CurrentObjective, "first lesson pending");
            // Legacy adapter is retained, but selecting a mode is not a successful attack.
            Input.ParseInputEvent(new InputEventAction { Action = InputActions.SelectAttackMode, Pressed = true });
            Input.FlushBufferedEvents();
            await KeyboardActionRuntimeTests.Frames(tree, input, 3);
            Input.ParseInputEvent(new InputEventAction { Action = InputActions.SelectAttackMode, Pressed = false });
            Input.FlushBufferedEvents();
            TestAssert.Equal(PrologueObjective.LearnAttackMode, completion.CurrentObjective,
                "ModeOnlyCannotCompleteAttackLesson: a legacy mode edge is not an accepted attack");

            await Pulse(tree, input, Key.Key2);
            float before = energy.EnergyRatio;
            await Pulse(tree, input, Key.J);
            TestAssert.True(modes.ActiveSlot == 2 && modes.LastRejection == PlayerActionRejectionReason.EmptySlot &&
                attacks == 0 && energy.EnergyRatio >= before && completion.CurrentObjective == PrologueObjective.LearnAttackMode,
                "EmptySlotDoesNotSpend: rejected real J neither spends energy nor completes teaching");
            await Pulse(tree, input, Key.Key1);
            await Pulse(tree, input, Key.J);
            TestAssert.True(attacks == 1 && completion.CurrentObjective == PrologueObjective.LearnGrappleMode,
                "accepted physical J completes attack lesson exactly once");
            await KeyboardActionRuntimeTests.Frames(tree, input, 35);
            Input.ParseInputEvent(new InputEventAction { Action = InputActions.SelectGrappleMode, Pressed = true });
            Input.FlushBufferedEvents();
            await KeyboardActionRuntimeTests.Frames(tree, input, 3);
            Input.ParseInputEvent(new InputEventAction { Action = InputActions.SelectGrappleMode, Pressed = false });
            Input.FlushBufferedEvents();
            TestAssert.Equal(PrologueObjective.LearnGrappleMode, completion.CurrentObjective,
                "mode selection alone cannot complete the claw lesson");
            var ice = new Area2D { Name = "TeachingIceBlocker", CollisionLayer = CombatCollisionLayers.ThreadTarget,
                CollisionMask = 0, Monitoring = false };
            ice.AddChild(new CollisionShape2D { Shape = new RectangleShape2D { Size = new Vector2(100, 8) } });
            ice.AddChild(new GrappleSurface2D { Name = "GrappleSurfaceProfile", Material = GrappleSurfaceMaterial.Ice,
                UseCollisionHitPoint = true });
            level.AddChild(ice); ice.GlobalPosition = player.GlobalPosition + Vector2.Down * 24f;
            KeyboardActionRuntimeTests.KeyEvent(Key.S, true);
            await KeyboardActionRuntimeTests.Frames(tree, input, 3);
            TestAssert.True(!new MechanicalClawTargetResolver().ResolveKeyboard(player, Vector2.Down).Snapshot.Exists,
                "controlled ice first hit genuinely has no valid downward candidate");
            await Pulse(tree, input, Key.K);
            await KeyboardActionRuntimeTests.Frames(tree, input, 40);
            KeyboardActionRuntimeTests.KeyEvent(Key.S, false);
            TestAssert.Equal(PrologueObjective.LearnGrappleMode, completion.CurrentObjective,
                "a failed K into ice is not a successful grab");
            ice.QueueFree(); await KeyboardActionRuntimeTests.Frames(tree, input, 2);

            // Local diagnostic anchor, not route-completion evidence. It belongs to the
            // actual resident sector so the normal claw residency contract also runs.
            var anchor = new GrappleSurface2D { Name = "KeyboardTeachingAnchor" };
            level.CurrentSectorRoot.AddChild(anchor);
            anchor.GlobalPosition = player.GlobalPosition + Vector2.Up * 170f;
            KeyboardActionRuntimeTests.KeyEvent(Key.W, true);
            await KeyboardActionRuntimeTests.Frames(tree, input, 3);
            await Pulse(tree, input, Key.K);
            for (int tick = 0; tick < 60 && completion.CurrentObjective == PrologueObjective.LearnGrappleMode; tick++)
                await KeyboardActionRuntimeTests.Frames(tree, input, 1);
            TestAssert.True(claw.CurrentTarget == anchor && claw.CurrentFrame.State is
                MechanicalClawState.Latched or MechanicalClawState.Reel or MechanicalClawState.Swing &&
                completion.CurrentObjective == PrologueObjective.ReachBasin,
                "AcceptedClawCompletesLesson: physical K must attach to the valid locked target");
            await Pulse(tree, input, Key.K);
            KeyboardActionRuntimeTests.KeyEvent(Key.W, false);
            await KeyboardActionRuntimeTests.Frames(tree, input, 90);
            anchor.QueueFree();

            var prompt = new TutorialPromptTrigger { Name = "KeyboardPromptFixture", CollisionLayer = 0,
                CollisionMask = 0, Monitoring = false };
            level.CurrentSectorRoot.AddChild(prompt);
            TestAssert.True(prompt.HasMeta("task10_continuous_runtime_bound"),
                "diagnostic prompt uses normal streaming-node subscription");
            foreach (string id in new[] { "attack", "thread_anchor", "thread_enemy" })
            {
                prompt.EmitSignal(TutorialPromptTrigger.SignalName.PromptRequested, new StringName(id), "fallback");
                foreach (GameLanguage language in Enum.GetValues<GameLanguage>())
                {
                    level.SetLanguage(language);
                    var model = level.GetNode<PrologueFormalHud>("Hud/FormalHud").CurrentModel;
                    RejectRetiredHelp(model.PromptGlyph + model.PromptText, $"streamed {id}/{language}");
                    TestAssert.True(model.PromptGlyph.Contains(id == "attack" ? "J" : "K"),
                        "actual streamed prompt presents the independent keyboard action");
                }
            }
            prompt.QueueFree();

            // Preserve stable old objective/tutorial identifiers across a real isolated
            // save and Continue. Prompt-dedup IDs must not be repurposed as action flags.
            completion.RestoreSession(PrologueObjective.EnterTemple);
            foreach (string id in new[] { "attack", "thread_anchor", "move" })
                level.Progress.CompleteTutorial(id);
            flow.PersistProgress("keyboard-old-tutorial-compatibility");
            flow.ReturnToTitle(); flow.Continue();
            await KeyboardActionRuntimeTests.Frames(tree, input, 3);
            TestAssert.True(completion.CurrentObjective == PrologueObjective.EnterTemple &&
                level.Progress.CompletedTutorials.Contains("attack") &&
                level.Progress.CompletedTutorials.Contains("thread_anchor"),
                "OldTutorialRemainsCompleted: Continue retains canonical old IDs and later objective");
            completion.RestoreSession(PrologueObjective.LearnShield);
            await Pulse(tree, input, Key.L);
            TestAssert.Equal(PrologueObjective.ClimbCanyon, completion.CurrentObjective,
                "accepted physical L rising guard completes shield lesson");
            GD.Print("KEYBOARD_TEACHING_PROGRESS_OK");
        }
        finally
        {
            foreach (Key key in new[] { Key.W, Key.S, Key.J, Key.K, Key.L, Key.Key1, Key.Key2 })
                KeyboardActionRuntimeTests.KeyEvent(key, false);
            foreach (StringName action in new[] { InputActions.SelectAttackMode, InputActions.SelectGrappleMode })
                Input.ParseInputEvent(new InputEventAction { Action = action, Pressed = false });
            Input.FlushBufferedEvents(); tree.Paused = false;
            level.QueueFree(); await Frames(tree, 3);
        }
        return paths.CaseRoot;
    }

    public static async Task RunSampleGuide(SceneTree tree, string storage)
    {
        foreach (GameLanguage language in Enum.GetValues<GameLanguage>())
        {
            var entry = GD.Load<PackedScene>("res://scenes/prologue/sample/BasinQualitySample.tscn")
                .Instantiate<BasinQualitySampleEntry>();
            // The validated owning case came from Configure before the preceding full-level
            // fixture was mounted. Keep this legacy sample in a separate retained child slot.
            entry.StorageDirectory = System.IO.Path.Combine(storage, "sample-" + language);
            tree.Root.AddChild(entry); await Frames(tree, 5);
            try
            {
                var level = entry.Level;
                var flow = level.GetNode<PrologueGameFlowController>("GameFlow");
                flow.ApplySettings(new GameSettingsData { Language = language });
                flow.StartNew(); await Frames(tree, 10);
                var panel = level.GetNode<Control>("SampleGuide/View/Panel");
                void Check(string context)
                {
                    string text = string.Empty;
                    float previousBottom = panel.GetGlobalRect().Position.Y;
                    foreach (string name in new[] { "Title", "Keys", "Body" })
                    {
                        var label = panel.GetNode<Label>(name);
                        text += label.Text;
                        TestAssert.True(label.GetMinimumSize().X <= label.Size.X + 1 &&
                            label.GetMinimumSize().Y <= label.Size.Y + 1,
                            $"sample {language}/{context}/{name} text fits its real label");
                        // Compact combat intentionally hides Body outside the reduced card.
                        if (!label.IsVisibleInTree()) continue;
                        Rect2 rectangle = label.GetGlobalRect();
                        TestAssert.True(panel.GetGlobalRect().Encloses(rectangle) &&
                            rectangle.Position.Y + 1f >= previousBottom,
                            $"sample {language}/{context}/{name} stays inside the panel and below preceding text: " +
                            $"label={rectangle}, previousBottom={previousBottom}, panel={panel.GetGlobalRect()}");
                        previousBottom = rectangle.End.Y;
                    }
                    RejectRetiredHelp(text, $"sample {language}/{context}");
                }
                Check("normal combat");
                var enemy = level.CurrentSectorRoot.GetNode<BellBackLaborer>("Gameplay/Actors/BellBackLaborer");
                TestAssert.True(enemy.TryReceiveThread(default), "controlled real receiver opens armor for UI fixture");
                await Frames(tree, 3); Check("exposed");
                var input = level.Player.GetNode<PlayerInputReader>("Input");
                await Pulse(tree, input, Key.Key2); Check("empty slot");
                await Pulse(tree, input, Key.Key1);
                KeyboardActionRuntimeTests.KeyEvent(Key.L, true);
                await KeyboardActionRuntimeTests.Frames(tree, input, 5); await Frames(tree, 2);
                Check("active guard");
                TestAssert.True(panel.GetNode<Label>("Keys").Text.Contains("L"), "sample held guard teaches release L");
                KeyboardActionRuntimeTests.KeyEvent(Key.L, false);
                // Controlled lethal fixture is solely for the post-combat teaching branch.
                var health = enemy.GetNode<HealthComponent>("Health");
                TestAssert.True(health.ApplyDamage(new DamageRequest(health.CurrentHealth, Vector2.Zero, 0f, 998UL)),
                    "controlled enemy defeat is accepted for sample route-card test");
                await Frames(tree, 4); Check("upper route");
                string keys = panel.GetNode<Label>("Keys").Text;
                TestAssert.True(keys.Contains("K") && keys.Contains("SPACE"), "sample climb reference teaches K and Space");

                // Exercise the longer attached card through an actual terrain cast, not reflected guide state.
                var claw = level.Player.GetNode<MechanicalClawController>("MechanicalClaw");
                var entryCatch = level.CurrentSectorRoot.GetNode<StaticBody2D>("Gameplay/Collision/EntryCatch");
                KeyboardActionRuntimeTests.KeyEvent(Key.W, true);
                await KeyboardActionRuntimeTests.Frames(tree, input, 2);
                await Pulse(tree, input, Key.K);
                for (int frame = 0; frame < 90 && !(claw.CurrentTarget?.GetParent() == entryCatch &&
                     claw.CurrentFrame.State is MechanicalClawState.Reel or MechanicalClawState.Swing); frame++)
                    await Frames(tree, 1);
                TestAssert.True(claw.CurrentTarget?.GetParent() == entryCatch &&
                    claw.CurrentFrame.State is MechanicalClawState.Reel or MechanicalClawState.Swing,
                    $"sample {language} keyboard cast actually attaches to the authored upper terrain");
                await Frames(tree, 2);
                Check("attached upper route");
                TestAssert.True(panel.GetNode<Label>("Title").Text.Contains(
                    language == GameLanguage.English ? "Connected" : "已抓牢", StringComparison.Ordinal),
                    "real terrain lock presents the attached card rather than its shorter default");
                GD.Print($"KEYBOARD_SAMPLE_ATTACHED_LAYOUT_OK language={language} state={claw.CurrentFrame.State}");
            }
            finally
            {
                foreach (Key key in new[] { Key.L, Key.W, Key.K, Key.Key1, Key.Key2 }) KeyboardActionRuntimeTests.KeyEvent(key, false);
                tree.Paused = false;
                if (GodotObject.IsInstanceValid(entry.Level))
                    await TestSceneTeardown.FreeSceneAfterAudioDrain(tree, entry.Level);
                entry.QueueFree(); await Frames(tree, 3);
            }
        }
        GD.Print("KEYBOARD_SAMPLE_GUIDE_OK");
    }

    private static async Task Pulse(SceneTree tree, PlayerInputReader input, Key key)
    {
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        KeyboardActionRuntimeTests.KeyEvent(key, true);
        await KeyboardActionRuntimeTests.Frames(tree, input, 3);
        KeyboardActionRuntimeTests.KeyEvent(key, false);
        await KeyboardActionRuntimeTests.Frames(tree, input, 2);
    }

    // Catches a live controls screen advertising mode selection/mouse buttons after
    // the actual keyboard input contract has changed, plus clipped/overlapping rows.
    public static async Task RunUi(SceneTree tree)
    {
        var viewport = new SubViewport { Size = new Vector2I(1280, 720) };
        var ui = GD.Load<PackedScene>("res://scenes/ui/PrologueUi.tscn").Instantiate<PrologueUiRoot>();
        tree.Root.AddChild(viewport);
        viewport.AddChild(ui);
        Node level = GD.Load<PackedScene>("res://scenes/prologue/PrologueLevel.tscn").Instantiate();
        var pause = level.GetNode<ProloguePauseMenu>("PauseMenu");
        var owners = CaptureTeachingOwners("ui", level, ui, viewport, pause);
        level.RemoveChild(pause); level.Free();
        ReportTeachingOwners("ui-after-offtree-main-free", owners);
        viewport.AddChild(pause);
        await Frames(tree, 3);
        try
        {
            foreach (GameLanguage language in Enum.GetValues<GameLanguage>())
            {
                ui.SetLanguage(language); ui.SetLastInputDevice(InputDeviceKind.KeyboardMouse);
                ui.ShowControls(); await Frames(tree, 3);
                Node rows = ui.GetNode("ControlsView/Panel/Rows");
                TestAssert.True(rows.GetNode<Label>("AttackMode").Text.Contains("[J]"),
                    "KeyboardHelpMatchesRuntime: live Attack row teaches J without a mode prerequisite");
                TestAssert.True(rows.GetNode<Label>("GrappleMode").Text.Contains("[K]"),
                    "live claw row teaches K");
                TestAssert.True(rows.GetNode<Label>("Shield").Text.Contains("[L]"), "live shield row teaches L");
                TestAssert.True(rows.GetNodeOrNull<Label>("Interact")?.Text.Contains("[Q]") == true,
                    "live environmental interaction row teaches Q");
                TestAssert.True(rows.GetNodeOrNull<Label>("Aim")?.Text.Contains("[W / A / S / D]") == true,
                    "shared keyboard aim is explicitly visible");
                string jet = rows.GetNode<Label>("Dash").Text;
                TestAssert.True(jet.Contains("SHIFT", StringComparison.OrdinalIgnoreCase) &&
                    jet.Contains(language == GameLanguage.English ? "Short Jet" : "短喷") &&
                    jet.Contains("A") && jet.Contains("D") &&
                    jet.Contains(language == GameLanguage.English ? "brake" : "制动", StringComparison.OrdinalIgnoreCase),
                    "actual controls row teaches short jet and opposite horizontal brake");
                string all = string.Empty;
                Control panel = ui.GetNode<Control>("ControlsView/Panel");
                Button back = ui.GetNode<Button>("ControlsView/Panel/Back");
                float bottom = rows.GetNode<Control>("Move").GetGlobalRect().Position.Y;
                foreach (Node node in rows.GetChildren())
                {
                    if (node is not Label row || !row.Visible) continue;
                    all += row.Text + "\n";
                    Rect2 rect = row.GetGlobalRect();
                    TestAssert.True(panel.GetGlobalRect().Encloses(rect) &&
                        rect.Position.Y >= bottom - 1f && !rect.Intersects(back.GetGlobalRect()) &&
                        row.GetMinimumSize().X <= row.Size.X + 1f && row.GetMinimumSize().Y <= row.Size.Y + 1f,
                        $"{language}/{row.Name} controls row fits without clipping or overlapping Back");
                    bottom = rect.End.Y;
                }
                RejectRetiredHelp(all, language.ToString());
                TestAssert.True(viewport.GuiGetFocusOwner() == back, "keyboard controls page focuses Back");
                pause.SetLanguage(language); pause.Open(); pause.Execute(ProloguePauseCommand.ShowControls);
                await Frames(tree, 2);
                string text = pause.GetNode<Label>("Panel/Controls").Text;
                TestAssert.True(text.Contains(language == GameLanguage.English ? "SHORT JET" : "短喷") &&
                    text.Contains(language == GameLanguage.English ? "BRAKE" : "制动"),
                    "actual pause page doesn't retain obsolete fixed-speed dash teaching");
                RejectRetiredHelp(text, $"{language} pause");
                foreach (string token in new[] { "J", "K", "L", "Q", "WASD", "SPACE", "ESC" })
                    TestAssert.True(text.Contains(token, StringComparison.OrdinalIgnoreCase),
                        $"{language} pause explains {token}");
                Label reference = pause.GetNode<Label>("Panel/Controls");
                TestAssert.True(pause.GetNode<Control>("Panel/ControlsWell").GetGlobalRect()
                    .Encloses(reference.GetGlobalRect()) && reference.GetMinimumSize().X <= reference.Size.X + 1f &&
                    reference.GetMinimumSize().Y <= reference.Size.Y + 1f, "pause reference fits its actual well");
                pause.Execute(ProloguePauseCommand.Resume);
            }
            GD.Print("KEYBOARD_TEACHING_UI_OK");
        }
        finally
        {
            tree.Paused = false; viewport.QueueFree(); await Frames(tree, 2);
            ReportTeachingOwners("ui-after-existing-teardown", owners);
        }
    }

    // Exercises the displayed guidance for every objective with the actual glyph resolver.
    public static void RunGuidance()
    {
        PrologueStrings.Load(FileAccess.GetFileAsString("res://assets/localization/prologue-ui.json"));
        var glyphs = new Dictionary<StringName, string>();
        foreach (StringName action in new[] { InputActions.MoveLeft, InputActions.MoveRight, InputActions.MoveUp,
            InputActions.MoveDown, InputActions.Jump, InputActions.Dash, InputActions.Attack, InputActions.Claw, InputActions.Interact,
            InputActions.KeyboardShield, InputActions.Slot1, InputActions.Slot2, InputActions.Slot3, InputActions.Slot4,
            InputActions.SelectAttackMode, InputActions.SelectGrappleMode, InputActions.PrimaryAction, InputActions.Shield })
            glyphs[action] = InputGlyphResolver.Resolve(InputDeviceKind.KeyboardMouse, action.ToString()).Text;
        foreach (GameLanguage language in Enum.GetValues<GameLanguage>())
        foreach (PrologueObjective objective in Enum.GetValues<PrologueObjective>())
        {
            var context = new PrologueGuidanceContext(objective, PrologueSectorId.LostTemple,
                PrologueBossGuidanceState.Inactive, 2, language, true);
            PrologueGuidanceInstruction result = PrologueGuidanceModel.Evaluate(context, glyphs);
            RejectRetiredHelp(result.StepText + result.HintText, $"{language}/{objective}");
            TestAssert.True(!result.StepText.Contains('{'), "guidance has no unresolved action tokens");
        }
        GD.Print("KEYBOARD_TEACHING_GUIDANCE_OK");
    }

    private static void RejectRetiredHelp(string text, string context)
    {
        foreach (string token in new[] { "LMB", "RMB", "MOUSE", "鼠标", "左键", "右键", "SELECT ATTACK", "SELECT CLAW", "选择攻击", "选择机械爪", "切换机械爪", "Q/" })
            TestAssert.True(!text.Contains(token, StringComparison.OrdinalIgnoreCase),
                $"{context}: formal keyboard help cannot require obsolete {token}");
    }

    private static async Task Frames(SceneTree tree, int count)
    { for (int i = 0; i < count; i++) await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame); }
}
