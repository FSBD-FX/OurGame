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
            GD.Print($"THRUSTER_TEACHING_PREPARATION_BOUNDARY state={flow.State} pending={level.ResidentPreparationPending} input_sequence={input.CurrentFrameSequence}");
            // The normal session disables input during atomic resident preparation.
            // Do not spend the original eight sampled-input observations in Intro.
            await BasinAscentCameraRuntimeTests.WaitForPlaying(tree, level);
            await KeyboardActionRuntimeTests.Frames(tree, input, 8);
            var completion = level.GetNode<PrologueCompletionController>("Completion");
            var guidance = level.GetNode<PrologueGuidanceController>("Guidance");
            var visual = level.GetNode<WorldTargetMarkerVisual>("Hud/Guidance/VisualRoot");
            TestAssert.True(flow.State == PrologueGameFlowState.Playing && level.Player.IsOnFloor() &&
                completion.CurrentObjective == PrologueObjective.LearnAttackMode && guidance.SameSectorFailureCount == 0,
                "real supported first spawn has not attacked, failed, or skipped the initial lesson");
            var glyphs = new Dictionary<StringName, string>();
            foreach (StringName action in new[] { InputActions.MoveLeft, InputActions.MoveRight,
                InputActions.MoveUp, InputActions.MoveDown, InputActions.Jump, InputActions.Dash,
                InputActions.Attack, InputActions.Claw, InputActions.KeyboardShield,
                InputActions.Slot1, InputActions.Slot2, InputActions.Slot3, InputActions.Slot4 })
                glyphs[action] = InputGlyphResolver.Resolve(InputDeviceKind.KeyboardMouse, action.ToString()).Text;
            foreach (GameLanguage language in Enum.GetValues<GameLanguage>())
            {
                guidance.SetLanguage(language); guidance.RefreshNow(); await Frames(tree, 3);
                var context = new PrologueGuidanceContext(PrologueObjective.LearnAttackMode,
                    PrologueSectorId.CanyonEntrance, PrologueBossGuidanceState.Inactive, 0, language, true);
                var combo = PrologueGuidanceModel.Evaluate(context with
                    { RecyclingLesson = PrologueRecyclingLesson.Combination }, glyphs);
                TestAssert.True(guidance.CurrentInstruction == combo && visual.IsExpandedTeaching &&
                    visual.HasVisiblePrompt && visual.RenderedTextContained &&
                    visual.RenderedStepText == combo.StepText &&
                    visual.GetNode<Label>("Text").Text.Contains(combo.StepText),
                    "real initial spawn preserves complete Combo warning ahead of the pending ordinary lesson");

                // Explicit renderer probe for an ordinary context: no moving the
                // real actor or disabling a device to manufacture spawn priority.
                // Synchronous presentation is immediately restored by RefreshNow.
                var ordinary = PrologueGuidanceModel.Evaluate(context, glyphs);
                visual.SetExpandedTeaching(false);
                visual.Present(ordinary, Array.Empty<PrologueGuidanceTargetView>());
                string text = ordinary.StepText;
                TestAssert.True(text.Contains("SHIFT", StringComparison.OrdinalIgnoreCase) &&
                    text.Contains(language == GameLanguage.English ? "SHORT JET" : "短喷") &&
                    text.Contains("A") && text.Contains("D") && visual.IsVisibleInTree() &&
                    visual.HasVisiblePrompt && visual.RenderedTextContained && visual.RenderedStepText == text &&
                    visual.GetNode<Label>("Text").Text.Contains(text),
                    "explicit ordinary model card teaches short jet/braking on mounted renderer without clipping");
                guidance.RefreshNow();
                TestAssert.True(guidance.CurrentInstruction == combo && visual.IsExpandedTeaching &&
                    visual.HasVisiblePrompt && visual.RenderedTextContained && visual.RenderedStepText == combo.StepText &&
                    completion.CurrentObjective == PrologueObjective.LearnAttackMode &&
                    guidance.SameSectorFailureCount == 0,
                    "ordinary renderer probe cannot alter the real Combo priority or complete the pending objective");
            }
            await RunFirstJetContext(tree, level, guidance, visual);
            GD.Print("THRUSTER_TEACHING_UI_OK real_initial_combo=true ordinary_renderer_probe=true no_failure_required=true visible_card_contained=true headless_layout_only=true human=false");
        }
        finally
        {
            ThrusterPlayerRuntimeTests.Release(); tree.Paused = false;
            var owners = CaptureTeachingOwners("entry", level);
            level.Free(); await Frames(tree, 2);
            ReportTeachingOwners("entry-after-existing-teardown", owners);
        }
    }

    // Break caught: an unused first jet must be taught by the production
    // controller, not merely by directly presenting an ordinary model card.
    private static async Task RunFirstJetContext(SceneTree tree, PrologueLevelController level,
        PrologueGuidanceController guidance, WorldTargetMarkerVisual visual)
    {
        var player = level.Player;
        var input = player.GetNode<PlayerInputReader>("Input");
        var motor = player.GetNode<PlayerMotor>("Motor");
        var flow = level.GetNode<PrologueGameFlowController>("GameFlow");
        var pause = level.GetNode<ProloguePauseMenu>("PauseMenu");
        var body = player.GetNode<CollisionShape2D>("BodyCollider");
        StaticBody2D support = level.CurrentSectorRoot.GetNode<StaticBody2D>("Gameplay/Collision/BridgeFarBank");
        var supportShape = support.GetNode<CollisionShape2D>("Collision");
        TestAssert.True(level.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors").LayoutVersion ==
            CanyonWorldLayout.CurrentLayoutVersion && body.Shape is CapsuleShape2D && !body.Disabled &&
            supportShape.Shape is RectangleShape2D && !supportShape.Disabled,
            "FirstJetContext uses layout2's saved enabled player body and original far-bank collision");
        float floorTop = supportShape.GlobalPosition.Y - ((RectangleShape2D)supportShape.Shape).Size.Y / 2f;
        TestAssert.True(Mathf.Abs(floorTop - 636f) < .01f &&
            Mathf.Abs(((CapsuleShape2D)body.Shape).Height / 2f - 36f) < .01f,
            "FirstJetContext fixture verifies the authored support top and original player half-height");
        bool OnOriginalSupport()
        {
            for (int index = 0; player.IsOnFloor() && index < player.GetSlideCollisionCount(); index++)
            {
                using KinematicCollision2D collision = player.GetSlideCollision(index);
                if (collision.GetCollider() == support) return true;
            }
            return false;
        }
        async Task Tick()
        { await KeyboardActionRuntimeTests.Frames(tree, input, 2); await Frames(tree, 1); }
        async Task Arrange()
        {
            // Bounded fixture setup, not route evidence: retain all original
            // enemies, mechanisms and collision; never write velocity as proof.
            support = level.CurrentSectorRoot.GetNode<StaticBody2D>("Gameplay/Collision/BridgeFarBank");
            player.GlobalPosition = new Vector2(1100f, 590f); motor.ResetFallTracking();
            int stable = 0;
            for (int tick = 0; tick < 60 && stable < 3; tick++)
            {
                await KeyboardActionRuntimeTests.Frames(tree, input, 1);
                stable = OnOriginalSupport() ? stable + 1 : 0;
            }
            TestAssert.True(stable == 3 && level.CurrentSector == PrologueSectorId.CanyonEntrance &&
                flow.State == PrologueGameFlowState.Playing,
                $"FirstJetContext fixture naturally settles on original far bank; player={player.GlobalPosition}");
            await Tick();
        }
        var jetTexts = new HashSet<string>(StringComparer.Ordinal);
        string expectedDashGlyph = "SHIFT";
        bool IsJet() => guidance.CurrentInstruction.ContextTitle is "ECHO · THRUSTER" or "ECHO · 推进器";
        void Prompt(string context)
        {
            var instruction = guidance.CurrentInstruction;
            TestAssert.True(instruction.Visible && instruction.ContextTitle ==
                (guidance.Language == GameLanguage.English ? "ECHO · THRUSTER" : "ECHO · 推进器"),
                $"FirstJetContext unused first jet automatically owns the supported production card: " +
                $"{context}, language={guidance.Language}, title={instruction.ContextTitle}, text={instruction.StepText}");
            foreach (StringName action in new[] { InputActions.Dash, InputActions.MoveLeft, InputActions.MoveRight })
                TestAssert.True(instruction.StepText.Contains(action == InputActions.Dash ? expectedDashGlyph :
                    InputGlyphResolver.Resolve(InputDeviceKind.KeyboardMouse, action.ToString()).Text,
                    StringComparison.OrdinalIgnoreCase),
                    "FirstJetContext automatic short-jet card uses the real live keyboard glyph " + action);
            TestAssert.True(instruction.StepText.Contains(expectedDashGlyph +
                (guidance.Language == GameLanguage.English ? ":" : "："), StringComparison.Ordinal) &&
                (expectedDashGlyph.Contains("SHIFT", StringComparison.Ordinal) || !instruction.StepText.Contains("SHIFT", StringComparison.Ordinal)),
                "FirstJetContext uses the fixture's actual dash binding as the control prefix, not default glyph metadata");
            TestAssert.True(instruction.StepText.Contains(guidance.Language == GameLanguage.English ? "SHORT JET" : "短喷",
                StringComparison.OrdinalIgnoreCase) && instruction.Targets.Count == 0 && instruction.HintText.Length == 0 &&
                visual.RenderedTargetCount == 0 && visual.IsVisibleInTree() && visual.HasVisiblePrompt &&
                visual.RenderedTextContained && visual.RenderedStepText == instruction.StepText &&
                visual.GetNode<Label>("Text").Text.Contains(instruction.StepText, StringComparison.Ordinal),
                context + ": automatic short-jet card fits the existing reading area without a world target or hint");
            jetTexts.Add(instruction.StepText);
        }
        void NoJet(string context) => TestAssert.True(!IsJet() && !jetTexts.Contains(visual.RenderedStepText),
            context + ": first-jet context and its stale rendered copy are absent");
        async Task AcceptedJet(string context)
        {
            int before = motor.AcceptedDashCount; ulong cursor = motor.ThrusterPresentationSequence;
            await Pulse(tree, input, Key.V); await Tick();
            var state = motor.ThrusterState;
            bool validStart = false;
            foreach (var fact in motor.ReadThrusterTransitionsAfter(cursor))
                validStart |= fact.Kind == Knight.Presentation.ThrusterTransitionKind.Started &&
                    fact.Identity.OwnerId == motor.GetInstanceId() && fact.Identity.Lifecycle == state.Lifecycle &&
                    fact.Identity.ConfigurationEpoch == state.ConfigurationEpoch && fact.Identity.BurnId != 0;
            TestAssert.True(motor.AcceptedDashCount == before + 1 && validStart,
                context + ": real remapped V produces one fresh Motor-owned Started with current lifecycle/epoch");
            NoJet(context);
        }
        await Arrange();
        foreach (GameLanguage language in Enum.GetValues<GameLanguage>())
        { guidance.SetLanguage(language); await Tick(); Prompt("initial " + language); }
        pause.Open(); await Frames(tree, 2);
        TestAssert.True(tree.Paused && flow.State == PrologueGameFlowState.Paused &&
            !guidance.CurrentInstruction.Visible && !visual.HasVisiblePrompt,
            "FirstJetContext real pause withdraws an unlearned card");
        pause.Execute(ProloguePauseCommand.Resume); await Tick(); Prompt("resume before success");

        var originalDashBindings = InputMap.ActionGetEvents(InputActions.Dash);
        var observer = new ThrusterPostMotorObserver { ProcessPhysicsPriority = 100 };
        level.AddChild(observer);
        try
        {
            InputMap.ActionEraseEvents(InputActions.Dash);
            using var chord = new InputEventKey { Keycode = Key.V, CtrlPressed = true };
            InputMap.ActionAddEvent(InputActions.Dash, chord);
            expectedDashGlyph = "CTRL+V";
            bool chordShown = true;
            var chordEvidence = new List<string>();
            foreach (GameLanguage language in Enum.GetValues<GameLanguage>())
            {
                guidance.SetLanguage(language); await Tick();
                string prefix = expectedDashGlyph + (language == GameLanguage.English ? ":" : "：");
                string actual = visual.GetNode<Label>("Text").Text;
                bool shown = IsJet() && visual.HasVisiblePrompt && actual.Contains(prefix, StringComparison.Ordinal);
                chordEvidence.Add($"{language}={actual.Replace('\n', ' ')}");
                chordShown &= shown;
                if (shown) Prompt("live Ctrl+V remap " + language);
            }
            TestAssert.True(chordShown,
                "FIRST_JET_COMBINATION_GLYPH_RED: visible first-jet card must show Ctrl+V in both languages; " + string.Join(" | ", chordEvidence));

            KeyboardActionRuntimeTests.KeyEvent(Key.L, true); await Tick();
            TestAssert.True(player.GetNode<PlayerShieldController>("Shield").IsActive,
                "modifier observation retains unlearned guidance via the real shield");
            int beforeChord = motor.AcceptedDashCount;
            bool bareDash = false, chordDash = false, guardedChord = false;
            observer.Observe = () => bareDash |= input.RawFrame.DashPressed;
            await Pulse(tree, input, Key.V);
            observer.Observe = () =>
            {
                chordDash |= input.RawFrame.DashPressed;
                guardedChord |= input.RawFrame.DashPressed && input.RawFrame.ShieldHeld;
            };
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            using (var press = new InputEventKey { Keycode = Key.V, PhysicalKeycode = Key.V, CtrlPressed = true, Pressed = true })
            {
                Input.ParseInputEvent(press); Input.FlushBufferedEvents();
                await KeyboardActionRuntimeTests.Frames(tree, input, 3);
            }
            using (var release = new InputEventKey { Keycode = Key.V, PhysicalKeycode = Key.V, CtrlPressed = true, Pressed = false })
            {
                Input.ParseInputEvent(release); Input.FlushBufferedEvents();
                await KeyboardActionRuntimeTests.Frames(tree, input, 2);
            }
            observer.Observe = null;
            KeyboardActionRuntimeTests.KeyEvent(Key.L, false); await Tick();
            TestAssert.True(!bareDash && chordDash && guardedChord && motor.AcceptedDashCount == beforeChord && !motor.IsDashing,
                $"FirstJetContext actual InputMap chord agrees with its visible prefix: bare={bareDash} chord={chordDash} shield_same_sample={guardedChord}");

            InputMap.ActionEraseEvents(InputActions.Dash);
            using var sided = new InputEventKey { PhysicalKeycode = Key.Shift, Location = KeyLocation.Right };
            InputMap.ActionAddEvent(InputActions.Dash, sided);
            foreach (GameLanguage language in Enum.GetValues<GameLanguage>())
            {
                guidance.SetLanguage(language);
                expectedDashGlyph = language == GameLanguage.English ? "RIGHT SHIFT" : "右 SHIFT";
                await Tick(); Prompt("live right Shift remap " + language);
            }
            GD.Print("FIRST_JET_LIVE_CHORD_OK languages=2 rendered_label=true bare_v_rejected=true chord_raw=true shield_same_sample=true right_shift=true default_preserved=true human=false");

            InputMap.ActionEraseEvents(InputActions.Dash);
            using var remappedDash = new InputEventKey { PhysicalKeycode = Key.V, Keycode = Key.V };
            InputMap.ActionAddEvent(InputActions.Dash, remappedDash);
            expectedDashGlyph = "V";
            await Tick(); Prompt("live V remap");
            KeyboardActionRuntimeTests.KeyEvent(Key.L, true); await Tick();
            TestAssert.True(player.GetNode<PlayerShieldController>("Shield").IsActive,
                "FirstJetContext refusal fixture actually raises the original shield with L");
            int accepted = motor.AcceptedDashCount; bool rawGuardedPress = false;
            observer.Observe = () => rawGuardedPress |= input.RawFrame.DashPressed && input.RawFrame.ShieldHeld;
            await Pulse(tree, input, Key.V); observer.Observe = null;
            TestAssert.True(rawGuardedPress && motor.AcceptedDashCount == accepted && !motor.IsDashing,
                "FirstJetContext real held L plus remapped V reaches raw input but cannot teach a rejected short jet");
            KeyboardActionRuntimeTests.KeyEvent(Key.L, false); await Tick();
            await Arrange(); Prompt("guarded V rejection preserves unlearned card");
            await AcceptedJet("first accepted V retires card");
            motor.ResetTransientState(); await Arrange(); NoJet("ordinary motor reset preserves learned state");
            pause.Open(); await Frames(tree, 2); pause.Execute(ProloguePauseCommand.Resume);
            await Tick(); NoJet("pause after success preserves learned state");
            TestAssert.True(motor.ReadThrusterTransitionsAfter(0).Count > 0,
                "FirstJetContext new-session case retains a genuine old journal to reject, not an empty test double");
            flow.ReturnToTitle(); flow.StartNew(); await BasinAscentCameraRuntimeTests.WaitForPlaying(tree, level);
            TestAssert.True(level.Player == player && player.GetNode<PlayerMotor>("Motor") == motor,
                "FirstJetContext StartNew exercises a fresh session over the same real Motor journal");
            await Arrange(); Prompt("new session ignores old Started and teaches again");
            guidance.SetGuidanceEnabled(false); await Tick();
            TestAssert.True(!guidance.CurrentInstruction.Visible && !visual.HasVisiblePrompt,
                "FirstJetContext guidance switch hides the card without manufacturing success");
            await AcceptedJet("real success while guidance disabled");
            guidance.SetGuidanceEnabled(true); motor.ResetTransientState();
            await Arrange(); NoJet("re-enabled guidance remembers actual disabled-period success");
            GD.Print("FIRST_JET_CONTEXT_OK automatic_context=true actual_support=true bilingual=true remap_V=true " +
                "guarded_raw_rejected=true accepted_identity=true pause=true reset_preserves=true " +
                "new_session_baseline=true disabled_success=true controlled_fixture=true natural_route=false human=false");
        }
        finally
        {
            observer.Observe = null; observer.QueueFree();
            KeyboardActionRuntimeTests.KeyEvent(Key.V, false); KeyboardActionRuntimeTests.KeyEvent(Key.L, false);
            InputMap.ActionEraseEvents(InputActions.Dash);
            foreach (InputEvent binding in originalDashBindings) InputMap.ActionAddEvent(InputActions.Dash, binding);
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
                Node rows = ui.GetNode("ControlsView/Panel/Scroll/Rows");
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
            await CheckLiveControlsBindings(tree, ui, pause);
            GD.Print("KEYBOARD_TEACHING_UI_OK");
        }
        finally
        {
            tree.Paused = false; viewport.QueueFree(); await Frames(tree, 2);
            ReportTeachingOwners("ui-after-existing-teardown", owners);
        }
    }

    public static async Task RunHighScaleTitleReference(SceneTree tree)
    {
        var paths = IsolatedPrologueFixture.CreatePaths("controls-high-scale");
        var level = PrologueGameFlowTests.Instantiate("HighScaleControlsTitle", paths.SavePath);
        var flow = level.GetNode<PrologueGameFlowController>("GameFlow");
        flow.AutomationAutoStart = false;
        Vector2I oldSize = tree.Root.Size, oldContentSize = tree.Root.ContentScaleSize;
        float oldScale = tree.Root.ContentScaleFactor;
        try
        {
            tree.Root.AddChild(level);
            await Frames(tree, 4);
            TestAssert.True(flow.State == PrologueGameFlowState.Title, "scaled reference starts from the actual title");
            tree.Root.Size = new Vector2I(1280, 720);
            tree.Root.ContentScaleSize = new Vector2I(1280, 720);
            var ui = level.GetNode<PrologueUiRoot>("ProductUi");
            var panel = ui.GetNode<Control>("ControlsView/Panel");
            var controlsButton = ui.GetNode<Button>("MainMenu/Panel/Buttons/Controls");
            var back = panel.GetNode<Button>("Back");
            var heading = panel.GetNode<Label>("Title");

            async Task Tap(Key key)
            {
                using var press = new InputEventKey { Keycode = key, PhysicalKeycode = key, Pressed = true };
                using var release = new InputEventKey { Keycode = key, PhysicalKeycode = key, Pressed = false };
                tree.Root.PushInput(press, true);
                try { await Frames(tree, 2); }
                finally { tree.Root.PushInput(release, true); }
                await Frames(tree, 2);
            }

            foreach (GameLanguage language in Enum.GetValues<GameLanguage>())
            {
                GameSettingsData settings = flow.Settings.Normalize();
                settings.WindowMode = GameWindowMode.Windowed;
                settings.ResolutionWidth = 1280;
                settings.ResolutionHeight = 720;
                settings.UiScale = 1.5f;
                settings.Language = language;
                flow.ApplySettings(settings);
                await Frames(tree, 4);
                Rect2 screen = tree.Root.GetVisibleRect();
                TestAssert.Near(1.5f, tree.Root.ContentScaleFactor, .001f, "real settings apply 150%");
                TestAssert.Near(1280f / 1.5f, screen.Size.X, 2f, "actual 720p scaled logical width");
                TestAssert.Near(480f, screen.Size.Y, 2f, "actual 720p scaled logical height");
                controlsButton.EmitSignal(Button.SignalName.Pressed);
                await Frames(tree, 4);
                Rect2 fixedBack = back.GetGlobalRect(), fixedTitle = heading.GetGlobalRect();
                TestAssert.True(back.IsVisibleInTree() && tree.Root.GuiGetFocusOwner() == back &&
                    screen.Encloses(fixedBack) && screen.Encloses(fixedTitle),
                    $"CONTROLS_HIGH_SCALE_CLIPS_BACK {language} screen={screen} back={fixedBack} title={fixedTitle}");
                var scroll = panel.GetNode<ScrollContainer>("Scroll");
                var first = scroll.GetNode<Label>("Rows/Move");
                var last = scroll.GetNode<Label>("Rows/Pause");
                TestAssert.True(scroll.ScrollVertical == 0 && scroll.GetGlobalRect().Encloses(first.GetGlobalRect()) &&
                    !scroll.GetGlobalRect().Encloses(last.GetGlobalRect()) &&
                    first.GetThemeFontSize("font_size") == 18 &&
                    scroll.GetNode<Label>("Rows/GrappleMode").GetThemeFontSize("font_size") == 16,
                    "scaled page starts with the first row readable, original font sizes, and real overflow");
                await Tap(Key.Down);
                TestAssert.True(scroll.ScrollVertical > 0, "direction key scrolls the actual body with Back focused");
                await Tap(Key.Pageup);
                TestAssert.Equal(0, scroll.ScrollVertical, "PageUp returns to the first row");
                for (int i = 0; i < 3 && !scroll.GetGlobalRect().Encloses(last.GetGlobalRect()); i++)
                    await Tap(Key.Pagedown);
                TestAssert.True(scroll.ScrollVertical > 0 && scroll.GetGlobalRect().Encloses(last.GetGlobalRect()) &&
                    back.GetGlobalRect() == fixedBack && heading.GetGlobalRect() == fixedTitle &&
                    tree.Root.GuiGetFocusOwner() == back,
                    "physical PageDown exposes the last whole label with fixed title and Back");
                await Tap(Key.Enter);
                TestAssert.True(controlsButton.IsVisibleInTree() && !back.IsVisibleInTree() &&
                    tree.Root.GuiGetFocusOwner() == ui.GetNode<Button>("MainMenu/Panel/Buttons/Start"),
                    "physical Enter on fixed Back returns to the actual title and restores Start focus");
                controlsButton.EmitSignal(Button.SignalName.Pressed);
                await Frames(tree, 4);
                TestAssert.True(scroll.ScrollVertical == 0 && scroll.GetGlobalRect().Encloses(first.GetGlobalRect()),
                    "opening the reference again resets reading position");
                await Tap(Key.Enter);
            }
            GD.Print("CONTROLS_HIGH_SCALE_OK production_scale=1.5 physical=1280x720 bilingual=true fixed_back=true physical_scroll=true native_back=true");
        }
        finally
        {
            await TestSceneTeardown.FreeSceneAfterAudioDrain(tree, level);
            tree.Root.Size = oldSize;
            tree.Root.ContentScaleSize = oldContentSize;
            tree.Root.ContentScaleFactor = oldScale;
        }
    }

    public static async Task RunHighScaleTitleSettings(SceneTree tree)
    {
        var paths = IsolatedPrologueFixture.CreatePaths("settings-high-scale");
        var level = PrologueGameFlowTests.Instantiate("HighScaleSettingsTitle", paths.SavePath);
        var flow = level.GetNode<PrologueGameFlowController>("GameFlow");
        flow.AutomationAutoStart = false;
        Vector2I oldSize = tree.Root.Size, oldContentSize = tree.Root.ContentScaleSize;
        float oldScale = tree.Root.ContentScaleFactor;
        try
        {
            tree.Root.AddChild(level);
            await Frames(tree, 4);
            TestAssert.True(flow.State == PrologueGameFlowState.Title, "settings reference starts from actual title");
            tree.Root.Size = new Vector2I(1280, 720);
            tree.Root.ContentScaleSize = new Vector2I(1280, 720);
            var ui = level.GetNode<PrologueUiRoot>("ProductUi");
            var panel = ui.GetNode<Control>("SettingsMenu/Panel");
            var open = ui.GetNode<Button>("MainMenu/Panel/Buttons/Settings");
            var back = panel.GetNode<Button>("Back");
            var title = panel.GetNode<Label>("Title");
            var guidance = level.GetNode<PrologueGuidanceController>("Guidance");
            string[] order = ["Master", "Music", "Effects", "Ambience", "UiVolume", "WindowMode", "Resolution",
                "VSync", "UiScale", "ScreenShake", "ReduceFlashes", "ReducedFog", "Guidance", "Language"];
            async Task Tap(Key key, bool shift = false)
            {
                using var press = new InputEventKey { Keycode = key, PhysicalKeycode = key, Pressed = true, ShiftPressed = shift };
                using var release = new InputEventKey { Keycode = key, PhysicalKeycode = key, Pressed = false, ShiftPressed = shift };
                tree.Root.PushInput(press, true);
                try { await Frames(tree, 2); }
                finally { tree.Root.PushInput(release, true); }
                await Frames(tree, 2);
            }
            async Task OpenFromTitle()
            {
                TestAssert.True(tree.Root.GuiGetFocusOwner() == ui.GetNode<Button>("MainMenu/Panel/Buttons/Start"),
                    "settings navigation begins at the title's actual Start focus");
                for (int i = 0; i < 5 && tree.Root.GuiGetFocusOwner() != open; i++) await Tap(Key.Tab);
                TestAssert.True(tree.Root.GuiGetFocusOwner() == open, "Tab from Start reaches Settings");
                await Tap(Key.Enter);
                TestAssert.True(!open.IsVisibleInTree() && back.IsVisibleInTree(), "Enter opens the actual settings page");
            }

            foreach (GameLanguage language in Enum.GetValues<GameLanguage>())
            {
                GameSettingsData settings = flow.Settings.Normalize();
                settings.WindowMode = GameWindowMode.Windowed;
                settings.ResolutionWidth = 1280;
                settings.ResolutionHeight = 720;
                settings.UiScale = 1.5f;
                settings.Language = language;
                settings.ReduceFlashes = false;
                settings.ReducedFog = true;
                // Exercise both directions while leaving the final title choice disabled.
                bool guidanceBefore = language == GameLanguage.English;
                settings.GuidanceEnabled = guidanceBefore;
                flow.ApplySettings(settings);
                await Frames(tree, 4);
                Rect2 screen = tree.Root.GetVisibleRect();
                TestAssert.Near(1.5f, tree.Root.ContentScaleFactor, .001f, "real settings apply 150%");
                TestAssert.Near(1280f / 1.5f, screen.Size.X, 2f, "actual 720p scaled logical width");
                TestAssert.Near(480f, screen.Size.Y, 2f, "actual 720p scaled logical height");
                TestAssert.True(open.IsVisibleInTree() && screen.Encloses(open.GetGlobalRect()),
                    "the real Settings entry button is visible before opening");
                await OpenFromTitle();
                Rect2 fixedBack = back.GetGlobalRect(), fixedTitle = title.GetGlobalRect();
                TestAssert.True(back.IsVisibleInTree() && title.IsVisibleInTree() &&
                    screen.Encloses(fixedBack) && screen.Encloses(fixedTitle),
                    $"SETTINGS_HIGH_SCALE_CLIPS_BACK {language} screen={screen} back={fixedBack} title={fixedTitle}");
                var scroll = panel.GetNode<ScrollContainer>("Scroll");
                var guidanceToggle = scroll.GetNodeOrNull<CheckButton>("Rows/Guidance/Value");
                TestAssert.True(guidanceToggle != null && guidanceToggle.IsVisibleInTree(),
                    $"SETTINGS_GUIDANCE_MISSING {language}: the ordinary title settings must expose a guidance checkbox");
                string guidanceLabel = language == GameLanguage.English ? "First-level guidance" : "第一关指导";
                TestAssert.Equal(guidanceLabel, scroll.GetNode<Label>("Rows/Guidance/Label").Text,
                    "the real guidance checkbox label identifies the setting in the selected language");
                TestAssert.True(guidanceToggle.ButtonPressed == guidanceBefore && guidance.GuidanceEnabled == guidanceBefore,
                    "the checkbox reflects the existing guidance choice on entry");
                Control Row(string name) => scroll.GetNode<Control>($"Rows/{name}");
                Control Value(string name) => Row(name).GetNode<Control>("Value");
                bool RowExposed(string name) => scroll.GetGlobalRect().Encloses(Row(name).GetGlobalRect());
                TestAssert.True(tree.Root.GuiGetFocusOwner() == Value("Master") && scroll.ScrollVertical == 0 &&
                    RowExposed("Master") && !RowExposed("Language"),
                    "first control is focused and exposed, with genuine overflow at 150%");
                var master = (HSlider)Value("Master");
                float oldMaster = (float)master.Value;
                await Tap(Key.Right);
                TestAssert.True(tree.Root.GuiGetFocusOwner() == master && master.Value > oldMaster,
                    "Right edits the focused slider without leaving it");
                await Tap(Key.Down);
                TestAssert.True(tree.Root.GuiGetFocusOwner() == Value("Music"), "Down focuses the next setting");
                await Tap(Key.Up);
                TestAssert.True(tree.Root.GuiGetFocusOwner() == master, "Up returns to the previous setting");

                foreach (string name in order[1..])
                {
                    await Tap(Key.Tab);
                    TestAssert.True(tree.Root.GuiGetFocusOwner() == Value(name) && RowExposed(name),
                        $"SETTINGS_FOCUS_ROW_NOT_EXPOSED {language}/{name} owner={tree.Root.GuiGetFocusOwner()?.GetPath()} " +
                        $"body={scroll.GetGlobalRect()} row={Row(name).GetGlobalRect()} offset={scroll.ScrollVertical}");
                    if (name == "ReduceFlashes")
                    {
                        await Tap(Key.Space);
                        TestAssert.True(((CheckButton)Value(name)).ButtonPressed &&
                            !GameSettingsStore.LoadOrDefault(paths.SettingsPath).ReduceFlashes,
                            "Space toggles the visible setting without prematurely saving the page");
                    }
                    else if (name == "Guidance")
                    {
                        await Tap(Key.Space);
                        TestAssert.True(guidanceToggle.HasFocus() && guidanceToggle.ButtonPressed != guidanceBefore &&
                            guidance.GuidanceEnabled == guidanceBefore &&
                            GameSettingsStore.LoadOrDefault(paths.SettingsPath).GuidanceEnabled == guidanceBefore,
                            "Space edits the focused guidance checkbox without applying or saving before Back");
                    }
                }
                TestAssert.True(scroll.ScrollVertical > 0 && back.GetGlobalRect() == fixedBack &&
                    title.GetGlobalRect() == fixedTitle, "last row scrolls independently of the header and Back");
                var languageChoice = (OptionButton)Value("Language");
                PopupMenu languagePopup = languageChoice.GetPopup();
                int nextLanguage = 1 - (int)language;
                await Tap(Key.Enter);
                TestAssert.True(languagePopup.Visible && languagePopup.GetFocusedItem() == (int)language,
                    "Enter on the scrolled language control opens and focuses the current option");
                await Tap(language == GameLanguage.ChineseSimplified ? Key.Down : Key.Up);
                TestAssert.True(languagePopup.Visible && languagePopup.GetFocusedItem() == nextLanguage,
                    "direction key navigates the actual option popup, not the parent scroll body");
                await Tap(Key.Enter);
                TestAssert.True(!languagePopup.Visible && languageChoice.Selected == nextLanguage &&
                    tree.Root.GuiGetFocusOwner() == languageChoice,
                    "confirm selects the alternate language and restores the parent option focus");
                await Tap(Key.Tab);
                TestAssert.True(tree.Root.GuiGetFocusOwner() == back, "Tab after the last row reaches fixed Back");
                await Tap(Key.Enter);
                GameSettingsData persisted = GameSettingsStore.LoadOrDefault(paths.SettingsPath);
                TestAssert.True(persisted.GuidanceEnabled != guidanceBefore &&
                    flow.Settings.GuidanceEnabled == persisted.GuidanceEnabled &&
                    guidance.GuidanceEnabled == persisted.GuidanceEnabled && !System.IO.File.Exists(paths.SavePath),
                    "Back saves and applies the edited guidance setting without creating a gameplay save");
                TestAssert.True(!back.IsVisibleInTree() && open.IsVisibleInTree() &&
                    tree.Root.GuiGetFocusOwner() == ui.GetNode<Button>("MainMenu/Panel/Buttons/Start") &&
                    persisted.ReduceFlashes && persisted.ReducedFog && persisted.Language == (GameLanguage)nextLanguage &&
                    persisted.UiScale == 1.5f && Mathf.IsEqualApprox(persisted.MasterVolume, (float)master.Value),
                    "Enter on Back applies the actual edited settings and returns focus to the title");

                await OpenFromTitle();
                TestAssert.True(scroll.ScrollVertical == 0 && RowExposed("Master") &&
                    tree.Root.GuiGetFocusOwner() == master && ((CheckButton)Value("ReduceFlashes")).ButtonPressed &&
                    guidanceToggle.ButtonPressed == persisted.GuidanceEnabled &&
                    scroll.GetNode<Label>("Rows/Guidance/Label").Text ==
                        (nextLanguage == (int)GameLanguage.English ? "First-level guidance" : "第一关指导") &&
                    title.Text == (nextLanguage == (int)GameLanguage.English ? "Settings" : "设置"),
                    "reopen resets position and restores persisted values");
                await Tap(Key.Tab, shift: true);
                TestAssert.True(tree.Root.GuiGetFocusOwner() == back,
                    "Shift+Tab from the first control reaches Back without traversing all rows");
                await Tap(Key.Enter);

                GameSettingsData normal = flow.Settings.Normalize();
                normal.UiScale = 1f;
                flow.ApplySettings(normal);
                await Frames(tree, 4);
                await OpenFromTitle();
                Rect2 normalScreen = tree.Root.GetVisibleRect();
                TestAssert.True(normalScreen.Encloses(panel.GetGlobalRect()) && scroll.ScrollVertical == 0,
                    "normal 720p keeps the complete settings panel inside the viewport");
                float bottom = float.NegativeInfinity;
                foreach (string name in order)
                {
                    Control row = Row(name);
                    var label = row.GetNode<Label>("Label");
                    TestAssert.True(RowExposed(name) && row.GetGlobalRect().Position.Y >= bottom &&
                        label.GetMinimumSize().X <= label.Size.X + 1f && label.GetMinimumSize().Y <= label.Size.Y + 1f,
                        $"SETTINGS_NORMAL_LAYOUT_ROW {normal.Language}/{name} panel={panel.GetGlobalRect()} " +
                        $"body={scroll.GetGlobalRect()} row={row.GetGlobalRect()} prev_bottom={bottom} " +
                        $"rows_min={scroll.GetNode<Control>("Rows").GetCombinedMinimumSize()} " +
                        $"label_min={label.GetMinimumSize()} label_size={label.Size}");
                    bottom = row.GetGlobalRect().End.Y;
                }
                await Tap(Key.Tab, shift: true);
                await Tap(Key.Enter);
            }

            GameLanguage unchangedLanguage = flow.Settings.Language;
            foreach (bool enabled in new[] { true, false })
            {
                await OpenFromTitle();
                var option = panel.GetNode<CheckButton>("Scroll/Rows/Guidance/Value");
                // Master → fixed Back → Language → Guidance, with no popup edit.
                for (int step = 0; step < 3; step++) await Tap(Key.Tab, shift: true);
                TestAssert.True(option.HasFocus() && option.ButtonPressed != enabled,
                    "the guide-only edit opens with the previously saved choice and actual keyboard focus");
                await Tap(Key.Space);
                await Tap(Key.Tab);
                await Tap(Key.Tab);
                TestAssert.True(back.HasFocus(), "the guide-only edit returns through the normal Back command");
                await Tap(Key.Enter);
                GameSettingsData stored = GameSettingsStore.LoadOrDefault(paths.SettingsPath);
                TestAssert.True(stored.Language == unchangedLanguage && ui.Language == unchangedLanguage &&
                    stored.GuidanceEnabled == enabled && flow.Settings.GuidanceEnabled == enabled &&
                    guidance.GuidanceEnabled == enabled && !System.IO.File.Exists(paths.SavePath),
                    "the guidance preference is independent of language and does not create a gameplay save");
            }

            TestAssert.True(tree.Root.GuiGetFocusOwner() == ui.GetNode<Button>("MainMenu/Panel/Buttons/Start"),
                "the actual Start button regains focus after leaving Settings");
            await Tap(Key.Enter);
            await BasinAscentCameraRuntimeTests.WaitForPlaying(tree, level);
            var input = level.Player.GetNode<PlayerInputReader>("Input");
            await KeyboardActionRuntimeTests.Frames(tree, input, 1);
            var visual = level.GetNode<WorldTargetMarkerVisual>("Hud/Guidance/VisualRoot");
            TestAssert.True(!GameSettingsStore.LoadOrDefault(paths.SettingsPath).GuidanceEnabled &&
                !flow.Settings.GuidanceEnabled && !guidance.GuidanceEnabled && !guidance.CurrentInstruction.Visible &&
                !visual.HasVisiblePrompt,
                "the actual new game commits and accepts input with the persisted guidance choice and no guidance card");
            GD.Print("SETTINGS_HIGH_SCALE_OK production_scale=1.5 physical=1280x720 bilingual=true rows=14 keyboard_title=true keyboard_edit=true fixed_back=true persisted=true popup=true normal_layout=true");
            GD.Print("SETTINGS_GUIDANCE_OK keyboard_toggle=true both_directions=true bilingual=true language_independent=true deferred_until_back=true isolated_settings=true gameplay_save_unchanged=true reopen=true actual_playing=true guidance_hidden=true");
        }
        finally
        {
            await TestSceneTeardown.FreeSceneAfterAudioDrain(tree, level);
            tree.Root.Size = oldSize;
            tree.Root.ContentScaleSize = oldContentSize;
            tree.Root.ContentScaleFactor = oldScale;
        }
    }

    public static async Task RunHighScalePause(SceneTree tree)
    {
        var paths = IsolatedPrologueFixture.CreatePaths("pause-high-scale");
        var level = PrologueGameFlowTests.Instantiate("HighScalePauseMain", paths.SavePath);
        var flow = level.GetNode<PrologueGameFlowController>("GameFlow");
        flow.AutomationAutoStart = false;
        Vector2I oldSize = tree.Root.Size, oldContentSize = tree.Root.ContentScaleSize;
        float oldScale = tree.Root.ContentScaleFactor;
        try
        {
            tree.Root.AddChild(level);
            await Frames(tree, 4);
            tree.Root.Size = new Vector2I(1280, 720);
            tree.Root.ContentScaleSize = new Vector2I(1280, 720);
            var pause = level.GetNode<ProloguePauseMenu>("PauseMenu");
            var panel = pause.GetNode<Control>("Panel");
            var title = panel.GetNode<Label>("Title");
            var subtitle = panel.GetNode<Label>("Subtitle");
            var resume = panel.GetNode<Button>("Resume");
            var show = panel.GetNode<Button>("ShowControls");
            var well = panel.GetNode<Control>("ControlsWell");
            var controls = panel.GetNode<Label>("Controls");
            var failure = panel.GetNode<Control>("QuitFailure");
            var failureMessage = failure.GetNode<Label>("Message");
            Button[] choices = [failure.GetNode<Button>("Retry"), failure.GetNode<Button>("ExitWithoutSaving"),
                failure.GetNode<Button>("Cancel")];
            Button[] buttons = [resume, show, panel.GetNode<Button>("ReturnToCheckpoint"),
                panel.GetNode<Button>("QuitToDesktop"), panel.GetNode<Button>("GuidanceToggle")];
            int quitCalls = 0;
            pause.QuitHandler = () => quitCalls++;
            ulong playerId = level.Player.GetInstanceId();
            var input = level.Player.GetNode<PlayerInputReader>("Input");
            var start = level.GetNode<Button>("ProductUi/MainMenu/Panel/Buttons/Start");
            async Task Pulse(Key key)
            {
                KeyboardActionRuntimeTests.KeyEvent(key, true);
                try { await Frames(tree, 1); }
                finally { KeyboardActionRuntimeTests.KeyEvent(key, false); }
                await Frames(tree, 2);
            }
            async Task GuiTap(Key key, bool shift = false)
            {
                using var press = new InputEventKey { Keycode = key, PhysicalKeycode = key, Pressed = true, ShiftPressed = shift };
                using var release = new InputEventKey { Keycode = key, PhysicalKeycode = key, Pressed = false, ShiftPressed = shift };
                tree.Root.PushInput(press, true);
                try { await Frames(tree, 2); }
                finally { tree.Root.PushInput(release, true); }
                await Frames(tree, 2);
            }

            TestAssert.True(flow.State == PrologueGameFlowState.Title && start.HasFocus(),
                "high-scale pause fixture starts at real focused Title");
            await Pulse(Key.Enter);
            await BasinAscentCameraRuntimeTests.WaitForPlaying(tree, level);
            await KeyboardActionRuntimeTests.Frames(tree, input, 8);
            TestAssert.True(level.Player.IsOnFloor(), "real pause fixture starts on actual support");

            foreach (float scale in new[] { 1.5f, 1f })
            foreach (GameLanguage language in new[] { GameLanguage.English, GameLanguage.ChineseSimplified })
            {
                GameSettingsData settings = flow.Settings.Normalize();
                settings.WindowMode = GameWindowMode.Windowed;
                settings.ResolutionWidth = 1280;
                settings.ResolutionHeight = 720;
                settings.UiScale = scale;
                settings.Language = language;
                flow.ApplySettings(settings);
                await Frames(tree, 4);
                Rect2 screen = tree.Root.GetVisibleRect();
                TestAssert.Near(scale, tree.Root.ContentScaleFactor, .001f, "real pause scale applied");
                TestAssert.Near(1280f / scale, screen.Size.X, 2f, "actual pause logical width");
                TestAssert.Near(720f / scale, screen.Size.Y, 2f, "actual pause logical height");

                byte[] saveBefore = System.IO.File.ReadAllBytes(paths.SavePath);
                await Pulse(Key.Escape);
                TestAssert.True(flow.State == PrologueGameFlowState.Paused && tree.Paused && pause.IsOpen &&
                    panel.IsVisibleInTree() && resume.HasFocus(), "physical Esc pauses real Playing and focuses Resume");
                ulong pausedSample = input.CurrentFrameSequence;
                foreach (Button button in buttons)
                {
                    Rect2 rect = button.GetGlobalRect();
                    Vector2 min = button.GetCombinedMinimumSize();
                    TestAssert.True(button.IsVisibleInTree() && screen.Encloses(rect) &&
                        panel.GetGlobalRect().Encloses(rect) && rect.Size.X + .5f >= min.X && rect.Size.Y + .5f >= min.Y,
                        $"PAUSE_BUTTON_OUTSIDE {scale}/{language}/{button.Name} rect={rect} minimum={min} panel={panel.GetGlobalRect()} screen={screen}");
                }
                for (int a = 0; a < buttons.Length; a++)
                for (int b = a + 1; b < buttons.Length; b++)
                {
                    Rect2 first = buttons[a].GetGlobalRect(), second = buttons[b].GetGlobalRect();
                    TestAssert.True(!first.Intersects(second),
                        $"PAUSE_BUTTON_OVERLAP {scale}/{language} {buttons[a].Name}={first} {buttons[b].Name}={second}");
                }
                Rect2 safe = new(screen.Position + new Vector2(48, 40), screen.Size - new Vector2(96, 80));
                TestAssert.True(safe.Encloses(panel.GetGlobalRect()),
                    $"PAUSE_SAFE_MARGIN {scale}/{language} panel={panel.GetGlobalRect()} safe={safe}");
                TestAssert.True(panel.GetGlobalRect().Encloses(title.GetGlobalRect()) &&
                    panel.GetGlobalRect().Encloses(subtitle.GetGlobalRect()) &&
                    !title.GetGlobalRect().Intersects(subtitle.GetGlobalRect()),
                    $"PAUSE_HEADING_LAYOUT {scale}/{language} title={title.GetGlobalRect()} subtitle={subtitle.GetGlobalRect()}");

                await GuiTap(Key.Tab);
                TestAssert.True(show.HasFocus(), "Tab from Resume reaches actual Controls button");
                await GuiTap(Key.Enter);
                Vector2 textMinimum = controls.GetMinimumSize();
                TestAssert.True(controls.Text == pause.ControlsText && controls.Text.Split('\n').Length == 7 &&
                    panel.GetGlobalRect().Encloses(well.GetGlobalRect()) && well.GetGlobalRect().Encloses(controls.GetGlobalRect()) &&
                    textMinimum.X <= controls.Size.X + .5f && textMinimum.Y <= controls.Size.Y + .5f,
                    $"PAUSE_CONTROLS_LAYOUT {scale}/{language} well={well.GetGlobalRect()} text={controls.GetGlobalRect()} minimum={textMinimum}");
                TestAssert.True(tree.Paused && input.CurrentFrameSequence == pausedSample,
                    "reading pause controls does not advance the player's physics samples");
                await GuiTap(Key.Tab, shift: true);
                TestAssert.True(resume.HasFocus(), "Shift+Tab returns to the real Resume button");
                foreach (Button next in new[] { show, buttons[2], buttons[4], buttons[3], resume })
                {
                    await GuiTap(Key.Tab);
                    TestAssert.True(next.HasFocus(), $"pause Tab follows the visible row order through {next.Name}");
                }
                await GuiTap(Key.Enter);
                await KeyboardActionRuntimeTests.Frames(tree, input, 1);
                TestAssert.True(flow.State == PrologueGameFlowState.Playing && !tree.Paused && !panel.Visible &&
                    level.Player.GetInstanceId() == playerId && input.CurrentFrameSequence > pausedSample,
                    "Enter resumes the same actual player and physics input");
                await Pulse(Key.Escape);
                TestAssert.True(tree.Paused && resume.HasFocus(), "reopen restores Resume focus");
                await Pulse(Key.Escape);
                TestAssert.True(flow.State == PrologueGameFlowState.Playing && !tree.Paused && !pause.IsOpen &&
                    saveBefore.AsSpan().SequenceEqual(System.IO.File.ReadAllBytes(paths.SavePath)),
                    "second Esc resumes without modifying the isolated save");

                await Pulse(Key.Escape);
                for (int i = 0; i < 4; i++) await GuiTap(Key.Tab);
                TestAssert.True(buttons[3].HasFocus(), "the real Quit control is reachable from Resume");
                using (var heldSave = new System.IO.FileStream(paths.SavePath, System.IO.FileMode.Open,
                           System.IO.FileAccess.Read, System.IO.FileShare.Read))
                {
                    await GuiTap(Key.Enter);
                    Rect2 dialog = failure.GetGlobalRect(), warning = failureMessage.GetGlobalRect();
                    TestAssert.True(pause.QuitFailureVisible && failure.IsVisibleInTree() && tree.Paused &&
                        choices[0].HasFocus() && quitCalls == 0 && panel.GetGlobalRect().Encloses(dialog) &&
                        dialog.Encloses(warning) && failureMessage.GetMinimumSize().X <= warning.Size.X + .5f &&
                        failureMessage.GetMinimumSize().Y <= warning.Size.Y + .5f,
                        $"PAUSE_MODAL_LAYOUT {scale}/{language} dialog={dialog} warning={warning} min={failureMessage.GetMinimumSize()}");
                    float right = dialog.Position.X;
                    foreach (Button choice in choices)
                    {
                        Rect2 choiceRect = choice.GetGlobalRect();
                        TestAssert.True(choice.IsVisibleInTree() && dialog.Encloses(choiceRect) &&
                            choiceRect.Position.X >= right && !choiceRect.Intersects(warning),
                            $"PAUSE_MODAL_CHOICE_LAYOUT {scale}/{language}/{choice.Name} dialog={dialog} choice={choiceRect}");
                        right = choiceRect.End.X;
                    }
                    await Pulse(Key.Escape);
                    TestAssert.True(!pause.QuitFailureVisible && tree.Paused && buttons[3].HasFocus() && quitCalls == 0,
                        "Escape cancels the actual failure only, restoring Quit focus without exiting or resuming");
                }
                await Pulse(Key.Escape);
                TestAssert.True(flow.State == PrologueGameFlowState.Playing && !tree.Paused && !pause.IsOpen &&
                    saveBefore.AsSpan().SequenceEqual(System.IO.File.ReadAllBytes(paths.SavePath)),
                    "cancel then resume preserves the original locked slot bytes");
            }
            GD.Print("PAUSE_HIGH_SCALE_OK physical=1280x720 scales=1,1.5 bilingual=true actual_pause=true keyboard_controls=true focus_cycle=5 resume=true safe_geometry=true modal=true save_unchanged=true");

            // A live setting change fits against the OLD viewport in SetLanguage first.
            // The resize notification must then fit against the NEW viewport, including
            // after _Ready has already run and this same node leaves/re-enters the tree.
            byte[] liveSaveBefore = System.IO.File.ReadAllBytes(paths.SavePath);
            void ApplyLiveSettings(float scale, GameLanguage language)
            {
                GameSettingsData settings = flow.Settings.Normalize();
                settings.UiScale = scale;
                settings.Language = language;
                flow.ApplySettings(settings);
            }
            ApplyLiveSettings(1f, GameLanguage.English);
            await Frames(tree, 4);
            await Pulse(Key.Escape);
            await GuiTap(Key.Tab);
            await GuiTap(Key.Enter);
            ulong livePausedSample = input.CurrentFrameSequence;
            int titleFont = title.GetThemeFontSize("font_size");
            int controlsFont = controls.GetThemeFontSize("font_size");
            void AssertLiveLayout(float scale, string stage)
            {
                Rect2 screen = tree.Root.GetVisibleRect();
                Rect2 safe = new(screen.Position + new Vector2(48, 40), screen.Size - new Vector2(96, 80));
                Rect2 panelRect = panel.GetGlobalRect();
                TestAssert.Near(1280f / scale, screen.Size.X, 2f, "live pause logical width");
                TestAssert.Near(720f / scale, screen.Size.Y, 2f, "live pause logical height");
                TestAssert.True(safe.Encloses(panelRect),
                    $"PAUSE_LIVE_SAFE_MARGIN stage={stage} scale={scale} panel={panelRect} safe={safe}");
                TestAssert.True(pause.IsOpen && tree.Paused && flow.State == PrologueGameFlowState.Paused &&
                    panel.IsVisibleInTree() && level.Player.GetInstanceId() == playerId &&
                    input.CurrentFrameSequence == livePausedSample,
                    "live pause layout preserves actual player and frozen gameplay samples");
                TestAssert.True(title.GetThemeFontSize("font_size") == titleFont &&
                    controls.GetThemeFontSize("font_size") == controlsFont,
                    "live layout does not shrink fonts to negate the scale preference");
                TestAssert.True(panelRect.Encloses(title.GetGlobalRect()) &&
                    panelRect.Encloses(subtitle.GetGlobalRect()) &&
                    !title.GetGlobalRect().Intersects(subtitle.GetGlobalRect()), "live pause heading fits");
                for (int a = 0; a < buttons.Length; a++)
                {
                    Rect2 rect = buttons[a].GetGlobalRect();
                    Vector2 minimum = buttons[a].GetCombinedMinimumSize();
                    TestAssert.True(buttons[a].IsVisibleInTree() && panelRect.Encloses(rect) &&
                        rect.Size.X + .5f >= minimum.X && rect.Size.Y + .5f >= minimum.Y,
                        $"live pause button fits {stage}/{buttons[a].Name}");
                    for (int b = a + 1; b < buttons.Length; b++)
                        TestAssert.True(!rect.Intersects(buttons[b].GetGlobalRect()), "live pause buttons do not overlap");
                }
                TestAssert.True(controls.Text == pause.ControlsText && controls.Text.Split('\n').Length == 7 &&
                    panelRect.Encloses(well.GetGlobalRect()) && well.GetGlobalRect().Encloses(controls.GetGlobalRect()) &&
                    controls.GetMinimumSize().X <= controls.Size.X + .5f &&
                    controls.GetMinimumSize().Y <= controls.Size.Y + .5f, "live pause controls remain readable");
            }
            foreach ((float scale, GameLanguage language) in new[]
                { (1.5f, GameLanguage.English), (1f, GameLanguage.ChineseSimplified),
                  (1.5f, GameLanguage.ChineseSimplified), (1f, GameLanguage.English) })
            {
                ApplyLiveSettings(scale, language);
                await Frames(tree, 4);
                AssertLiveLayout(scale, "initial-live");
            }
            for (int reentry = 0; reentry < 2; reentry++)
            {
                level.RemoveChild(pause);
                level.AddChild(pause);
                await Frames(tree, 2);
                foreach (float scale in new[] { 1.5f, 1f })
                {
                    ApplyLiveSettings(scale, scale > 1f ? GameLanguage.English : GameLanguage.ChineseSimplified);
                    await Frames(tree, 4);
                    AssertLiveLayout(scale, $"reentry-{reentry + 1}");
                }
            }
            using (var heldSave = new System.IO.FileStream(paths.SavePath, System.IO.FileMode.Open,
                       System.IO.FileAccess.Read, System.IO.FileShare.Read))
            {
                pause.Execute(ProloguePauseCommand.QuitToDesktop);
                TestAssert.True(pause.QuitFailureVisible && choices[0].HasFocus() && quitCalls == 0,
                    "reentered pause opens the real locked-save failure without quitting");
                foreach ((float scale, GameLanguage language) in new[]
                    { (1.5f, GameLanguage.English), (1f, GameLanguage.ChineseSimplified) })
                {
                    ApplyLiveSettings(scale, language);
                    await Frames(tree, 4);
                    AssertLiveLayout(scale, "failure-live");
                    Rect2 dialog = failure.GetGlobalRect(), warning = failureMessage.GetGlobalRect();
                    TestAssert.True(failure.IsVisibleInTree() && panel.GetGlobalRect().Encloses(dialog) &&
                        dialog.Encloses(warning) && failureMessage.GetMinimumSize().X <= warning.Size.X + .5f &&
                        failureMessage.GetMinimumSize().Y <= warning.Size.Y + .5f && choices[0].HasFocus(),
                        "live failure warning fits and preserves Retry focus");
                    float right = dialog.Position.X;
                    foreach (Button choice in choices)
                    {
                        Rect2 rect = choice.GetGlobalRect();
                        TestAssert.True(dialog.Encloses(rect) && rect.Position.X >= right &&
                            !rect.Intersects(warning) && choice.GetCombinedMinimumSize().X <= rect.Size.X + .5f &&
                            choice.GetCombinedMinimumSize().Y <= rect.Size.Y + .5f,
                            "live failure choices fit without overlap or text clipping");
                        right = rect.End.X;
                    }
                }
                await Pulse(Key.Escape);
                TestAssert.True(!pause.QuitFailureVisible && tree.Paused && buttons[3].HasFocus() && quitCalls == 0,
                    "reentered live failure cancels without leaving pause");
            }
            await Pulse(Key.Escape);
            await KeyboardActionRuntimeTests.Frames(tree, input, 1);
            TestAssert.True(flow.State == PrologueGameFlowState.Playing && !tree.Paused && !pause.IsOpen &&
                level.Player.GetInstanceId() == playerId && input.CurrentFrameSequence > livePausedSample &&
                liveSaveBefore.AsSpan().SequenceEqual(System.IO.File.ReadAllBytes(paths.SavePath)),
                "live scale and reentry resume the same player with unchanged isolated save");
            int reenteredCloseCalls = 0;
            pause.Closed += () => reenteredCloseCalls++;
            await Pulse(Key.Escape);
            TestAssert.True(pause.IsOpen && resume.HasFocus(), "reentered pause still opens on real Esc");
            await GuiTap(Key.Enter);
            TestAssert.True(reenteredCloseCalls == 1 && !pause.IsOpen && !tree.Paused &&
                flow.State == PrologueGameFlowState.Playing && level.Player.GetInstanceId() == playerId &&
                liveSaveBefore.AsSpan().SequenceEqual(System.IO.File.ReadAllBytes(paths.SavePath)),
                "reentry does not duplicate the actual Resume button callback or mutate the save");
            GD.Print("PAUSE_LIVE_LAYOUT_OK physical=1280x720 scales=1,1.5 bilingual=true reentries=2 no_request_ready=true paused_samples_frozen=true modal=true native_resume=true button_once=true save_unchanged=true headless_geometry_only=true human=false");
        }
        finally
        {
            tree.Paused = false;
            await TestSceneTeardown.FreeSceneAfterAudioDrain(tree, level);
            tree.Root.Size = oldSize;
            tree.Root.ContentScaleSize = oldContentSize;
            tree.Root.ContentScaleFactor = oldScale;
        }
    }

    private static async Task CheckLiveControlsBindings(SceneTree tree, PrologueUiRoot ui, ProloguePauseMenu pause)
    {
        (StringName Action, Key Key)[] replacements =
        [
            (InputActions.MoveLeft, Key.F), (InputActions.MoveRight, Key.G), (InputActions.MoveUp, Key.T),
            (InputActions.Jump, Key.B), (InputActions.Dash, Key.V), (InputActions.Attack, Key.N),
            (InputActions.Claw, Key.M), (InputActions.KeyboardShield, Key.H),
            (InputActions.Interact, Key.Y), (InputActions.Slot1, Key.C), (InputActions.Pause, Key.P)
        ];
        var original = new Dictionary<StringName, Godot.Collections.Array<InputEvent>>();
        foreach (var (action, _) in replacements) original.Add(action, InputMap.ActionGetEvents(action));
        var failures = new List<string>();
        Node rows = ui.GetNode("ControlsView/Panel/Scroll/Rows");
        Label reference = pause.GetNode<Label>("Panel/Controls");
        void Check(bool condition, string context, string observed)
        {
            if (!condition) failures.Add($"{context}: {observed.Replace("\n", " | ")}");
        }
        void OpenTitleControls()
        {
            ui.GetNode<Button>("ControlsView/Panel/Back").EmitSignal(Button.SignalName.Pressed);
            ui.GetNode<Button>("MainMenu/Panel/Buttons/Controls").EmitSignal(Button.SignalName.Pressed);
        }
        void ShowPauseControls()
        {
            pause.Open();
            pause.GetNode<Button>("Panel/ShowControls").EmitSignal(Button.SignalName.Pressed);
        }
        try
        {
            foreach (var (action, code) in replacements)
            {
                InputMap.ActionEraseEvents(action);
                using var key = new InputEventKey { PhysicalKeycode = code };
                InputMap.ActionAddEvent(action, key);
            }
            foreach (GameLanguage language in Enum.GetValues<GameLanguage>())
            {
                bool english = language == GameLanguage.English;
                ui.SetLanguage(language); pause.SetLanguage(language);
                OpenTitleControls(); ShowPauseControls(); await Frames(tree, 3);
                string Row(string name) => rows.GetNode<Label>(name).Text;
                string dash = Row("Dash"), aim = Row("Aim"), claw = Row("GrappleMode"), pauseText = reference.Text;
                Check(dash.Contains("[V]") && dash.Contains("F / G") && !dash.Contains("SHIFT"),
                    $"TITLE_STALE_JET/{language}", dash);
                Check(aim.Contains("[T / F / S / G]") && !aim.Contains("A/D"),
                    $"TITLE_STALE_AIM/{language}", aim);
                Check(claw.Contains("[M]") && claw.Contains("T/F/S/G") && claw.Contains("F/G") &&
                    claw.Contains(english ? "B leap-release" : "B 随时跃出") && claw.Contains("N/H") &&
                    !claw.Contains("WASD") && !claw.Contains("SPACE") && !claw.Contains("J/L"),
                    $"TITLE_STALE_CLAW/{language}", claw);
                foreach (var (row, token) in new[] { ("Move", "[F / G]"), ("Jump", "[B]"), ("AttackMode", "[N]"), ("Shield", "[H]"),
                    ("Interact", "[Y]"), ("Loadout", "[C / 2 / 3 / 4]"), ("Pause", "[P]") })
                    Check(Row(row).Contains(token), $"TITLE_STALE_ROW/{language}/{row}", Row(row));
                string[] lines = pauseText.Split('\n');
                Check(lines.Length == 7 && lines[0].Contains("T/F/S/G") && lines[0].Contains("F/G") &&
                    lines[1].Contains(english ? "V SHORT JET" : "V 短喷") && lines[1].Contains("F/G") &&
                    lines[2].Contains(english ? "N ATTACK" : "N 攻击") && lines[2].Contains(english ? "M CAST" : "M 发爪") &&
                    lines[3].Contains("N/H") && lines[3].Contains(english ? "B LEAP" : "B 跃出") &&
                    lines[4].Contains(english ? "H SHIELD" : "H 举盾") && lines[5].Contains("C/2/3/4") &&
                    lines[5].Contains(english ? "Y NEARBY" : "Y 近距") && lines[5].Contains(english ? "P PAUSE" : "P 暂停") &&
                    !pauseText.Contains("SHIFT") && !pauseText.Contains("A/D") && !pauseText.Contains("SPACE"),
                    $"PAUSE_STALE_KEYS/{language}", pauseText);
                Check(pause.GetNode<Control>("Panel/ControlsWell").GetGlobalRect().Encloses(reference.GetGlobalRect()) &&
                    reference.GetMinimumSize().X <= reference.Size.X + 1f &&
                    reference.GetMinimumSize().Y <= reference.Size.Y + 1f, $"PAUSE_LIVE_FIT/{language}", pauseText);
                Rect2 panel = ui.GetNode<Control>("ControlsView/Panel").GetGlobalRect();
                foreach (Node node in rows.GetChildren())
                    if (node is Label label && label.Visible)
                        Check(panel.Encloses(label.GetGlobalRect()) && label.GetMinimumSize().X <= label.Size.X + 1f &&
                            label.GetMinimumSize().Y <= label.Size.Y + 1f,
                            $"TITLE_LIVE_FIT/{language}/{label.Name}", label.Text);
                pause.Execute(ProloguePauseCommand.Resume);
            }
            // Mouse-only action must not be presented as a working keyboard key.
            InputMap.ActionEraseEvents(InputActions.Dash);
            using var mouseOnly = new InputEventMouseButton { ButtonIndex = MouseButton.Right };
            InputMap.ActionAddEvent(InputActions.Dash, mouseOnly);
            foreach (GameLanguage language in Enum.GetValues<GameLanguage>())
            {
                string unbound = language == GameLanguage.English ? "UNBOUND" : "未绑定";
                ui.SetLanguage(language); pause.SetLanguage(language);
                OpenTitleControls(); ShowPauseControls(); await Frames(tree, 2);
                string titleDash = rows.GetNode<Label>("Dash").Text;
                Check(titleDash.Contains($"[{unbound}]") && reference.Text.Split('\n')[1].StartsWith(unbound) &&
                    !titleDash.Contains("SHIFT"), $"NO_KEYBOARD_BINDING/{language}", titleDash + "\n" + reference.Text);
                pause.Execute(ProloguePauseCommand.Resume);
            }
            // First usable keyboard binding wins even when preceded by a non-keyboard entry.
            using var chord = new InputEventKey { Keycode = Key.V, CtrlPressed = true };
            using var alias = new InputEventKey { PhysicalKeycode = Key.U };
            InputMap.ActionAddEvent(InputActions.Dash, chord);
            InputMap.ActionAddEvent(InputActions.Dash, alias);
            OpenTitleControls(); ShowPauseControls(); await Frames(tree, 2);
            Check(rows.GetNode<Label>("Dash").Text.Contains("[CTRL+V]") &&
                reference.Text.Split('\n')[1].Contains("CTRL+V"), "FIRST_LOGICAL_CHORD",
                rows.GetNode<Label>("Dash").Text + "\n" + reference.Text);
            ui.SetLastInputDevice(InputDeviceKind.Gamepad);
            Check(rows.GetNode<Label>("Dash").Text.Contains("[B]") &&
                rows.GetNode<Label>("GrappleMode").Text.Contains("[Y + RB]") &&
                !rows.GetNode<Label>("GrappleMode").Text.Contains('\n'), "GAMEPAD_LEGEND_UNCHANGED",
                rows.GetNode<Label>("Dash").Text + "\n" + rows.GetNode<Label>("GrappleMode").Text);
            ui.SetLastInputDevice(InputDeviceKind.KeyboardMouse);
            Check(rows.GetNode<Label>("Dash").Text.Contains("[CTRL+V]"), "KEYBOARD_LEGEND_RETURNS",
                rows.GetNode<Label>("Dash").Text);
            pause.Execute(ProloguePauseCommand.Resume);

            // A binding scoped to one physical side cannot advertise both Shift keys.
            foreach (GameLanguage language in Enum.GetValues<GameLanguage>())
            {
                ui.SetLanguage(language); pause.SetLanguage(language);
                foreach (var (location, english, chinese) in new[] {
                    (KeyLocation.Left, "LEFT SHIFT", "左 SHIFT"),
                    (KeyLocation.Right, "RIGHT SHIFT", "右 SHIFT") })
                {
                    InputMap.ActionEraseEvents(InputActions.Dash);
                    using var sided = new InputEventKey { PhysicalKeycode = Key.Shift, Location = location };
                    InputMap.ActionAddEvent(InputActions.Dash, sided);
                    string expected = language == GameLanguage.English ? english : chinese;
                    OpenTitleControls(); ShowPauseControls(); await Frames(tree, 2);
                    string titleDash = rows.GetNode<Label>("Dash").Text;
                    Check(titleDash.Contains($"[{expected}]") && reference.Text.Split('\n')[1].StartsWith(expected),
                        $"KEY_LOCATION_STALE/{language}/{location}", titleDash + "\n" + reference.Text);
                    pause.Execute(ProloguePauseCommand.Resume);
                }
            }
        }
        finally
        {
            foreach (var (action, events) in original)
            {
                InputMap.ActionEraseEvents(action);
                foreach (InputEvent inputEvent in events) InputMap.ActionAddEvent(action, inputEvent);
            }
        }
        OpenTitleControls();
        pause.Open(); await Frames(tree, 2);
        Check(rows.GetNode<Label>("Dash").Text.Contains("[SHIFT]") &&
            rows.GetNode<Label>("Aim").Text.Contains("[W / A / S / D]"),
            "TITLE_DEFAULT_RESTORED", rows.GetNode<Label>("Dash").Text);
        Check(reference.Text.Contains("SHIFT") && reference.Text.Contains("WASD"),
            "PAUSE_REOPEN_REFRESH", reference.Text);
        pause.Execute(ProloguePauseCommand.Resume);
        TestAssert.True(failures.Count == 0, "LIVE_CONTROL_BINDINGS_MISMATCH\n" + string.Join("\n", failures));
        GD.Print("KEYBOARD_CONTROL_BINDINGS_OK title=true pause=true bilingual=true remapped_actions=11 defaults_restored=true cached_pause_refreshed=true unbound=true chord=true gamepad=true key_location=true");
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
