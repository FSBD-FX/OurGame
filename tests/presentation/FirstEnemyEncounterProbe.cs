using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Godot;
using Knight.Abilities;
using Knight.Combat;
using Knight.Enemies;
using Knight.Player;
using Knight.Presentation;
using Knight.Prologue;
using Knight.Prologue.Guidance;
using Knight.Prologue.Seamless;
using Knight.Settings;
using Knight.Shared;
using Knight.Tests.Integration;
using Knight.Ui;

namespace Knight.Tests.Presentation;

// Actual generated-main encounter observation, not an isolated actor or pose fixture.
public partial class FirstEnemyEncounterProbe : SceneTree
{
    private bool _assertFirstClawGuidance;

    public override async void _Initialize()
    {
        PrologueLevelController level = null;
        RustShellSquire enemy = null;
        HealthComponent health = null;
        DefendCounterObserver defend = null;
        AttackPresentationFrame attack = default;
        var phases = new HashSet<AttackPhase>();
        var captures = new HashSet<string>();
        var telegraphedAttacks = new HashSet<ulong>();
        var renderedActiveAttacks = new HashSet<ulong>();
        var recoveredAttacks = new HashSet<ulong>();
        var completedAttacks = new HashSet<ulong>();
        int completed = 0, hits = 0, activeSamples = 0;
        float maximumBeyond = 0f;
        ulong previousAttack = 0;
        AttackPhase previousPhase = AttackPhase.None;
        void OnAttack(AttackPresentationFrame frame)
        {
            attack = frame;
            defend?.ObserveAttack(frame.Timeline);
            AttackPhase phase = frame.Timeline.Phase;
            if (phase == previousPhase && frame.Timeline.AttackId == previousAttack) return;
            if (phase == AttackPhase.Telegraph) telegraphedAttacks.Add(frame.Timeline.AttackId);
            if (phase == AttackPhase.Recovery) recoveredAttacks.Add(frame.Timeline.AttackId);
            if (previousPhase == AttackPhase.Recovery &&
                enemy.CurrentState is RustShellState.Idle or RustShellState.Chase or RustShellState.Telegraph &&
                telegraphedAttacks.Contains(previousAttack) && renderedActiveAttacks.Contains(previousAttack) &&
                recoveredAttacks.Contains(previousAttack) && completedAttacks.Add(previousAttack)) completed++;
            phases.Add(phase);
            GD.Print($"FIRST_ENEMY_PHASE physics={Engine.GetPhysicsFrames()} process={Engine.GetProcessFrames()} " +
                $"id={frame.Timeline.AttackId} phase={phase} remaining={frame.Timeline.PhaseRemainingSeconds:0.000} " +
                $"enemy={enemy.GlobalPosition} shape={frame.Shape.Origin}/{frame.Shape.Extent}");
            previousPhase = phase;
            previousAttack = frame.Timeline.AttackId;
        }
        void OnDamage(int remaining, Vector2 impulse)
        {
            hits++;
            GD.Print($"FIRST_ENEMY_PLAYER_HIT physics={Engine.GetPhysicsFrames()} health={remaining} " +
                $"impulse={impulse} attack={attack.Timeline.AttackId} phase={attack.Timeline.Phase}");
        }
        try
        {
            if (DisplayServer.GetName() == "headless")
                throw new InvalidOperationException("This probe requires real automatic rendering, not headless.");
            string output = Array.Find(OS.GetCmdlineUserArgs(), value => value.StartsWith("--output="))?[9..];
            bool assertEnvelope = Array.Exists(OS.GetCmdlineUserArgs(), value => value == "--assert-tip-envelope");
            bool defendCounter = Array.Exists(OS.GetCmdlineUserArgs(), value => value == "--defend-counter");
            bool sameTickCounter = Array.Exists(OS.GetCmdlineUserArgs(), value => value == "--same-tick-counter");
            bool assertCombatGuidance = Array.Exists(OS.GetCmdlineUserArgs(), value => value == "--assert-combat-guidance");
            bool english = Array.Exists(OS.GetCmdlineUserArgs(), value => value == "--english");
            bool exitGuidance = Array.Exists(OS.GetCmdlineUserArgs(), value => value == "--exit-guidance");
            bool descentPlatforms = Array.Exists(OS.GetCmdlineUserArgs(), value => value == "--descent-platforms");
            bool descentFirstClaw = Array.Exists(OS.GetCmdlineUserArgs(), value => value == "--descent-first-claw");
            _assertFirstClawGuidance = Array.Exists(OS.GetCmdlineUserArgs(), value => value == "--assert-first-claw-guidance");
            if (sameTickCounter && !defendCounter) throw new ArgumentException("--same-tick-counter requires --defend-counter");
            if (assertCombatGuidance && !defendCounter) throw new ArgumentException("--assert-combat-guidance requires --defend-counter");
            if (exitGuidance && !defendCounter) throw new ArgumentException("--exit-guidance requires --defend-counter");
            if (descentPlatforms && !exitGuidance) throw new ArgumentException("--descent-platforms requires --exit-guidance");
            if (descentFirstClaw && !descentPlatforms) throw new ArgumentException("--descent-first-claw requires --descent-platforms");
            if (_assertFirstClawGuidance && !descentFirstClaw)
                throw new ArgumentException("--assert-first-claw-guidance requires --descent-first-claw");
            if (output != null)
            {
                if (!Path.IsPathFullyQualified(output)) throw new ArgumentException("--output must be an absolute directory");
                output = Path.GetFullPath(output);
                string project = Path.GetFullPath(ProjectSettings.GlobalizePath("res://")).TrimEnd('/', '\\') + Path.DirectorySeparatorChar;
                if ((output.TrimEnd('/', '\\') + Path.DirectorySeparatorChar).StartsWith(project, StringComparison.OrdinalIgnoreCase))
                    throw new ArgumentException("Capture output must be outside the project");
                Directory.CreateDirectory(output);
            }
            ReleaseAll();
            level = GD.Load<PackedScene>("res://scenes/prologue/PrologueLevel.tscn").Instantiate<PrologueLevelController>();
            CharacterBody2D player = level.GetNode<CharacterBody2D>("PlayerRoot/Player");
            player.Position = new Vector2(120, 574); // sole pre-tree spawn assignment
            PrologueGameFlowController flow = level.GetNode<PrologueGameFlowController>("GameFlow");
            string isolated = $"user://first-enemy-probe/{Guid.NewGuid():N}";
            flow.AutomationAutoStart = true;
            flow.SavePath = isolated + "/save.json";
            flow.SettingsPath = isolated + "/settings.json";
            Root.AddChild(level);
            for (int frame = 0; frame < 5; frame++) await ToSignal(this, SignalName.PhysicsFrame);
            if (english)
            {
                await ToSignal(this, SignalName.ProcessFrame);
                GameSettingsData settings = flow.Settings.Normalize();
                settings.Language = GameLanguage.English;
                flow.ApplySettings(settings);
                GD.Print("FIRST_ENEMY_LANGUAGE English via real flow settings; other settings preserved");
            }
            enemy = level.GetNode<RustShellSquire>("WorldRoot/Sectors/CanyonEntrance/Gameplay/Actors/RustShellSquire");
            Node2D weapon = enemy.GetNode<Node2D>("VisualRoot/Formal/Weapon");
            Hitbox hitbox = enemy.GetNode<Hitbox>("AttackHitbox");
            CollisionShape2D attackCollider = hitbox.GetNode<CollisionShape2D>("AttackCollider");
            health = player.GetNode<HealthComponent>("Health");
            enemy.AttackFrameChanged += OnAttack;
            health.Damaged += OnDamage;
            if (defendCounter) defend = new DefendCounterObserver(level, player, enemy, output, sameTickCounter, assertCombatGuidance);
            ulong startPhysics = Engine.GetPhysicsFrames(), jumpPhysics = 0;
            bool jumped = false, jumpHeld = false, movingRight = false;
            int settledApproachFrames = 0;
            for (int sample = 0; sample < 600 && (defend == null ? completed < 2 : !defend.Done); sample++)
            {
                // PostDraw observes the automatically updated formal transform and
                // captures that exact rendered frame; it never calls Present/Process.
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                ulong physics = Engine.GetPhysicsFrames();
                if (physics - startPhysics > 600) break;
                float distance = enemy.GlobalPosition.X - player.GlobalPosition.X;
                bool sameFloor = player.IsOnFloor() && enemy.IsOnFloor() &&
                    Math.Abs(player.GlobalPosition.Y - enemy.GlobalPosition.Y) < 12f;
                bool approach = !sameFloor || distance > 60f;
                if (approach != movingRight) { Send(InputActions.MoveRight, approach); movingRight = approach; }
                if (!jumped && player.IsOnFloor() && player.GlobalPosition.X >= 350f)
                {
                    Send(InputActions.Jump, true);
                    jumped = jumpHeld = true;
                    jumpPhysics = physics;
                    GD.Print($"FIRST_ENEMY_GAP_JUMP physics={physics} player={player.GlobalPosition}");
                }
                if (jumpHeld && physics - jumpPhysics >= 12) { Send(InputActions.Jump, false); jumpHeld = false; }
                defend?.Tick(attack.Timeline, sameFloor, distance);
                settledApproachFrames = sameFloor && Math.Abs(distance) <= 65f && Math.Abs(player.Velocity.X) < 1f
                    ? settledApproachFrames + 1 : 0;
                bool firstSettled = settledApproachFrames == 6 && captures.Add("settled-approach");
                if (sample % 60 == 0 || firstSettled)
                {
                    GD.Print($"FIRST_ENEMY_APPROACH process={Engine.GetProcessFrames()} physics={physics} " +
                        $"player={player.GlobalPosition} enemy={enemy.GlobalPosition} dx={distance:0.00} floor={sameFloor} health={health.CurrentHealth}");
                    GD.Print($"FIRST_ENEMY_RUNTIME state={enemy.CurrentState} frame={enemy.CurrentFrame} " +
                        $"mode={enemy.ProcessMode} can_process={enemy.CanProcess()} physics_enabled={enemy.IsPhysicsProcessing()} " +
                        $"tree_paused={Paused} flow={flow.State} enemy_parent_mode={enemy.GetParent().ProcessMode}");
                    Node2D sightTarget = GetFirstNodeInGroup("player") as Node2D;
                    if (sightTarget != null)
                    {
                        // Reproduce the old root-only exclusion query for diagnosis.
                        // production_visible separately reports the current actor query.
                        var ray = PhysicsRayQueryParameters2D.Create(enemy.GlobalPosition, sightTarget.GlobalPosition);
                        ray.CollideWithAreas = true;
                        ray.CollideWithBodies = true;
                        ray.Exclude = new Godot.Collections.Array<Rid> { enemy.GetRid() };
                        Godot.Collections.Dictionary hit = enemy.GetWorld2D().DirectSpaceState.IntersectRay(ray);
                        Node collider = hit.Count > 0 ? hit["collider"].AsGodotObject() as Node : null;
                        GD.Print($"FIRST_ENEMY_LOS_LEGACY_QUERY target={sightTarget.GetPath()} target_position={sightTarget.GlobalPosition} " +
                            $"expected_player={sightTarget == player} production_visible={enemy.HasLineOfSightTo(sightTarget)} " +
                            $"mask={ray.CollisionMask} has_hit={hit.Count > 0} collider={collider?.GetPath()} " +
                            $"collider_type={collider?.GetClass()} rid={(hit.Count > 0 ? hit["rid"].AsRid().ToString() : "none")} " +
                            $"hit_position={(hit.Count > 0 ? hit["position"].AsVector2() : Vector2.Zero)}");
                        ray.Exclude = new Godot.Collections.Array<Rid> { enemy.GetRid(), hitbox.GetRid() };
                        Godot.Collections.Dictionary withoutOwnAttack = enemy.GetWorld2D().DirectSpaceState.IntersectRay(ray);
                        Node nextCollider = withoutOwnAttack.Count > 0 ? withoutOwnAttack["collider"].AsGodotObject() as Node : null;
                        GD.Print($"FIRST_ENEMY_LOS_DIAGNOSTIC_ONLY exclude_attack={hitbox.GetPath()} attack_rid={hitbox.GetRid()} " +
                            $"next_collider={nextCollider?.GetPath()} " +
                            $"next_rid={(withoutOwnAttack.Count > 0 ? withoutOwnAttack["rid"].AsRid().ToString() : "none")} " +
                            $"next_position={(withoutOwnAttack.Count > 0 ? withoutOwnAttack["position"].AsVector2() : Vector2.Zero)}");
                    }
                    else GD.Print("FIRST_ENEMY_LOS no player group target");
                }
                if (firstSettled && output != null)
                {
                    using Image image = Root.GetTexture().GetImage();
                    if (image == null || image.IsEmpty() || image.SavePng(Path.Combine(output, "settled-approach.png")) != Error.Ok)
                        throw new InvalidOperationException("Could not capture the settled encounter viewport.");
                    GD.Print("FIRST_ENEMY_CAPTURE settled-approach.png");
                }
                AttackTimelineFrame timeline = attack.Timeline;
                float progress = timeline.PhaseDurationSeconds > 0f
                    ? 1f - timeline.PhaseRemainingSeconds / timeline.PhaseDurationSeconds : 0f;
                bool lateTelegraph = timeline.Phase == AttackPhase.Telegraph && progress >= .7f;
                if (timeline.Phase != AttackPhase.Active && !lateTelegraph) continue;
                if (attack.Shape.Kind != AttackShapeKind.Rectangle)
                    throw new InvalidOperationException("First squire attack no longer publishes rectangle geometry.");
                Vector2 tip = weapon.ToGlobal(new Vector2(38, -2)); // actual drawn RustWeapon endpoint
                Transform2D current = attackCollider.GlobalTransform;
                if (attackCollider.Shape is not RectangleShape2D rectangle)
                    throw new InvalidOperationException("First squire current collision is not a rectangle.");
                Vector2 extent = rectangle.Size * new Vector2(current.X.Length(), current.Y.Length()) * .5f;
                float beyond = BeyondRectangle(tip, current.Origin, extent, current.Rotation);
                float publishedBeyond = BeyondRectangle(tip, attack.Shape.Origin, attack.Shape.Extent, attack.Shape.RotationRadians);
                if (timeline.Phase == AttackPhase.Active)
                {
                    activeSamples++;
                    renderedActiveAttacks.Add(timeline.AttackId);
                    maximumBeyond = Mathf.Max(maximumBeyond, beyond);
                }
                GD.Print($"FIRST_ENEMY_RENDER process={Engine.GetProcessFrames()} physics={physics} id={timeline.AttackId} " +
                    $"phase={timeline.Phase} progress={progress:0.000} actor={enemy.GlobalPosition} facing={enemy.Facing} " +
                    $"tip={tip} shape_origin={attack.Shape.Origin} half_extent={attack.Shape.Extent} rotation={attack.Shape.RotationRadians:0.000} " +
                    $"current_rect_origin={current.Origin} current_half_extent={extent} current_rotation={current.Rotation:0.000} " +
                    $"published_origin_delta={current.Origin - attack.Shape.Origin} published_beyond={publishedBeyond:0.00} " +
                    $"current_beyond={beyond:0.00} player={player.GlobalPosition} health={health.CurrentHealth} monitoring={hitbox.Monitoring}");
                string capture = lateTelegraph ? "late-telegraph" : progress >= .35f ? "mid-active" : null;
                if (output != null && capture != null && captures.Add(capture))
                {
                    using Image image = Root.GetTexture().GetImage();
                    if (image == null || image.IsEmpty() || image.SavePng(Path.Combine(output, capture + ".png")) != Error.Ok)
                        throw new InvalidOperationException("Could not capture the actual encounter viewport.");
                }
            }
            GD.Print($"FIRST_ENEMY_SUMMARY completed={completed} active_render_samples={activeSamples} " +
                $"hits={hits} health={health.CurrentHealth} max_tip_beyond={maximumBeyond:0.00}");
            foreach (AttackPhase phase in new[] { AttackPhase.Telegraph, AttackPhase.Active, AttackPhase.Recovery })
                if (!phases.Contains(phase)) throw new InvalidOperationException($"Actual encounter never reached {phase}.");
            if (defend != null) defend.AssertOutcome(hits);
            else if (activeSamples == 0 || completed < 2) throw new InvalidOperationException("Bounded encounter did not render and complete two actual attacks.");
            // Opt-in contract only: active weapon endpoint stays within the current
            // damage rectangle plus 3px (half the drawn 6px ink stroke).
            if (assertEnvelope && maximumBeyond > 3f)
                throw new InvalidOperationException($"Active weapon tip exceeds damage envelope by {maximumBeyond:0.00}px (allowed 3px).");
            if (exitGuidance)
            {
                defend.Dispose();
                defend = null;
                enemy.AttackFrameChanged -= OnAttack;
                await ContinueToDescent(level, player, output, descentPlatforms, descentFirstClaw);
            }
            GD.Print("FIRST_ENEMY_ENCOUNTER_PROBE_OK");
            Quit(0);
        }
        catch (Exception exception) { GD.PushError("FIRST_ENEMY_ENCOUNTER_PROBE_FAIL: " + exception); Quit(1); }
        finally
        {
            ReleaseAll();
            defend?.Dispose();
            if (GodotObject.IsInstanceValid(enemy)) enemy.AttackFrameChanged -= OnAttack;
            if (GodotObject.IsInstanceValid(health)) health.Damaged -= OnDamage;
            if (GodotObject.IsInstanceValid(level)) level.QueueFree();
        }
    }

    private async Task ContinueToDescent(PrologueLevelController level, CharacterBody2D player, string output,
        bool descentPlatforms, bool descentFirstClaw)
    {
        var failures = new List<string>();
        int recoveries = 0;
        void OnRecovery(string reason) { recoveries++; GD.Print("FIRST_ENEMY_EXIT_RECOVERY " + reason); }
        level.RecoveryCompleted += OnRecovery;
        try
        {
            await ToSignal(this, SignalName.ProcessFrame);
            ReleaseAll();
            Send(InputActions.SelectGrappleMode, true);
            for (int i = 0; i < 2; i++) await ToSignal(this, SignalName.PhysicsFrame);
            await ToSignal(this, SignalName.ProcessFrame);
            Send(InputActions.SelectGrappleMode, false);
            for (int i = 0; i < 3; i++)
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            ObserveExitGuidance(level, player, "exit-after-E", output,
                PrologueSectorId.CanyonEntrance, "Gameplay/RouteMarkers/NextSeam", failures);

            // Authored AttackFloor (720..1280) and EntryShelf (1280..1560) share top y610.
            // Walking crosses their real sector seam; no synthetic sector/position/velocity changes.
            await ToSignal(this, SignalName.ProcessFrame);
            Send(InputActions.MoveRight, true);
            ulong start = Engine.GetPhysicsFrames();
            for (int sample = 0; sample < 600 && Engine.GetPhysicsFrames() - start <= 360; sample++)
            {
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                if (sample % 30 == 0)
                    GD.Print($"FIRST_ENEMY_EXIT_WALK physics={Engine.GetPhysicsFrames()} player={player.GlobalPosition} " +
                        $"sector={level.CurrentSector} floor={player.IsOnFloor()} health={player.GetNode<HealthComponent>("Health").CurrentHealth}");
                if (level.CurrentSector == PrologueSectorId.CanyonDescent) break;
            }
            Send(InputActions.MoveRight, false);
            for (int i = 0; i < 3; i++)
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            ObserveExitGuidance(level, player, "exit-descent", output,
                PrologueSectorId.CanyonDescent, "Gameplay/Collision/VisibleIceWall/GrappleSurfaceProfile", failures);
            if (descentPlatforms && level.CurrentSector == PrologueSectorId.CanyonDescent)
                await WalkDescentPlatforms(level, player, output, failures, descentFirstClaw);
            if (recoveries != 0) failures.Add($"exit walk unexpectedly recovered {recoveries} times");
            // Preserve the complete before/after walk and screenshots even when the old entrance target is missing.
            if (failures.Count > 0) throw new InvalidOperationException("Exit guidance: " + string.Join(" | ", failures));
            GD.Print("FIRST_ENEMY_EXIT_GUIDANCE_OK actual E then D crossed into CanyonDescent");
        }
        finally
        {
            level.RecoveryCompleted -= OnRecovery;
            ReleaseAll();
        }
    }

    private async Task WalkDescentPlatforms(PrologueLevelController level, CharacterBody2D player,
        string output, List<string> failures, bool firstClaw)
    {
        StaticBody2D[] supports = {
            level.CurrentSectorRoot.GetNode<StaticBody2D>("Gameplay/Collision/GripStoneCatchZone"),
            level.CurrentSectorRoot.GetNode<StaticBody2D>("Gameplay/Collision/GripCatchShelf") };
        var visual = level.GetNode<WorldTargetMarkerVisual>("Hud/Guidance/VisualRoot");
        Label text = visual.GetNode<Label>("Text");
        int[] overlaps = new int[2];
        float[] largestArea = new float[2];
        int unreadable = 0, samples = 0, recoveries = 0;
        int jumpsBefore = player.GetNode<PlayerMotor>("Motor").AcceptedJumpCount;
        void OnRecovery(string reason) { recoveries++; GD.Print("DESCENT_PLATFORM_RECOVERY " + reason); }
        void Observe()
        {
            samples++;
            bool readable = visual.IsVisibleInTree() && visual.HasVisiblePrompt && text.IsVisibleInTree() &&
                !string.IsNullOrWhiteSpace(text.Text) &&
                text.Text.Contains(level.GetNode<PrologueGuidanceController>("Guidance").CurrentInstruction.StepText) &&
                visual.PromptRect.Grow(.5f).Encloses(text.GetRect()) && text.GetMinimumSize().Y <= text.Size.Y + .5f;
            if (!readable) unreadable++;
            for (int i = 0; i < supports.Length; i++)
            {
                Rect2 band = ProjectPlatformTop(supports[i]);
                Rect2 intersection = band.Intersection(visual.PromptRect);
                float area = intersection.HasArea() ? intersection.Size.X * intersection.Size.Y : 0f;
                if (visual.HasVisiblePrompt && area > .01f) overlaps[i]++;
                largestArea[i] = Mathf.Max(largestArea[i], area);
                GD.Print($"DESCENT_PLATFORM_SAMPLE process={Engine.GetProcessFrames()} physics={Engine.GetPhysicsFrames()} " +
                    $"player={player.GlobalPosition} support={supports[i].GetPath()} top_band={band} " +
                    $"visible_prompt={visual.HasVisiblePrompt} prompt={visual.PromptRect} readable={readable} " +
                    $"intersection_area={area:0.00} overlap_samples={overlaps[i]} maximum_area={largestArea[i]:0.00}");
            }
        }
        void Capture(string stage)
        {
            if (output == null) return;
            using Image image = Root.GetTexture().GetImage();
            if (image == null || image.IsEmpty() || image.SavePng(Path.Combine(output, stage + ".png")) != Error.Ok)
                throw new InvalidOperationException("Could not capture " + stage);
        }
        level.RecoveryCompleted += OnRecovery;
        try
        {
            // Already resumed from PostDraw at the descent entrance: include the original obscured platform.
            Observe();
            Capture("descent-platforms-head");
            ulong start = Engine.GetPhysicsFrames();
            for (int index = 0; index < supports.Length; index++)
            {
                await ToSignal(this, SignalName.ProcessFrame);
                Send(InputActions.MoveRight, true);
                bool released = false;
                int stable = 0;
                ulong lastPhysics = ulong.MaxValue;
                for (int frame = 0; frame < 600 && Engine.GetPhysicsFrames() - start < 360 && stable < 6; frame++)
                {
                    await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                    Observe();
                    bool supported = HasUpwardContact(player, supports[index]);
                    ulong physics = Engine.GetPhysicsFrames();
                    if (physics != lastPhysics) { stable = supported ? stable + 1 : 0; lastPhysics = physics; }
                    if (supported && !released)
                    {
                        await ToSignal(this, SignalName.ProcessFrame);
                        Send(InputActions.MoveRight, false);
                        released = true;
                    }
                }
                GD.Print($"DESCENT_PLATFORM_LANDING index={index + 1} support={supports[index].GetPath()} " +
                    $"stable_physics={stable} player={player.GlobalPosition} velocity={player.Velocity} released_D={released}");
                Capture("descent-platforms-" + (index + 1));
                if (stable < 6)
                {
                    failures.Add($"natural D descent did not stabilize on {supports[index].Name} within the six-second route budget");
                    break;
                }
                if (firstClaw)
                {
                    await FirstClawFromCatchShelf(level, player, failures, Capture);
                    break;
                }
            }
            if (unreadable > 0) failures.Add($"descent guidance was hidden/empty or text did not fit on {unreadable}/{samples} rendered samples");
            for (int i = 0; i < supports.Length; i++)
                if (overlaps[i] > 0) failures.Add($"guidance covered {supports[i].Name} top band on {overlaps[i]}/{samples} samples, max area {largestArea[i]:0.00}px2");
            if (recoveries > 0) failures.Add($"natural platform descent triggered {recoveries} recoveries");
            if (player.GetNode<PlayerMotor>("Motor").AcceptedJumpCount != jumpsBefore)
                failures.Add("natural platform descent unexpectedly accepted a jump");
        }
        finally
        {
            level.RecoveryCompleted -= OnRecovery;
            await ToSignal(this, SignalName.ProcessFrame);
            Send(InputActions.MoveRight, false);
        }
    }

    private async Task FirstClawFromCatchShelf(PrologueLevelController level, CharacterBody2D player,
        List<string> failures, Action<string> capture)
    {
        var claw = player.GetNode<MechanicalClawController>("MechanicalClaw");
        var target = level.CurrentSectorRoot.GetNode<GrappleSurface2D>(
            "Gameplay/Collision/FrostVentCeiling/GrappleSurfaceProfile");
        var landing = level.CurrentSectorRoot.GetNode<StaticBody2D>("Gameplay/Collision/GripLanding");
        Vector2 cursor = new(1960f, 511.53f);
        Vector2 intendedHit = new(1950f, 516f);
        int jumps = player.GetNode<PlayerMotor>("Motor").AcceptedJumpCount;
        int casts = 0, latches = 0, releases = 0;
        ulong castId = 0;
        bool wrongTarget = false, boostSeen = false;
        MechanicalClawState previous = MechanicalClawState.Idle;
        void OnClaw(MechanicalClawFrame frame, GrappleSurface2D surface)
        {
            boostSeen |= frame.State == MechanicalClawState.Boost || frame.BoostConsumed;
            if (frame.State == MechanicalClawState.Windup && previous != frame.State)
            { casts++; castId = frame.CastId; }
            if (frame.State == MechanicalClawState.Latched && previous != frame.State) latches++;
            if (frame.State == MechanicalClawState.Released && previous != frame.State) releases++;
            if (frame.LockedTargetRuntimeId != 0UL)
                wrongTarget |= frame.LockedTargetRuntimeId != target.GetInstanceId() || frame.CastId != castId;
            if (frame.State != previous)
                GD.Print($"DESCENT_FIRST_CLAW_STATE physics={Engine.GetPhysicsFrames()} cast={frame.CastId} state={frame.State} " +
                    $"failure={frame.Failure} target={surface?.GetPath()} anchor={frame.AnchorPoint} rope={frame.RopeLength:0.0}");
            previous = frame.State;
        }
        void Log(string phase)
        {
            MechanicalClawFrame frame = claw.CurrentFrame;
            var instruction = level.GetNode<PrologueGuidanceController>("Guidance").CurrentInstruction;
            Vector2 screen = player.GetGlobalTransformWithCanvas() * player.ToLocal(intendedHit);
            GD.Print($"DESCENT_FIRST_CLAW phase={phase} physics={Engine.GetPhysicsFrames()} process={Engine.GetProcessFrames()} " +
                $"player={player.GlobalPosition} velocity={player.Velocity} floor={player.IsOnFloor()} " +
                $"range={player.GlobalPosition.DistanceTo(intendedHit):0.0}/620 intended_hit={intendedHit} screen_hit={screen} " +
                $"on_screen={player.GetViewportRect().HasPoint(screen)} state={frame.State} failure={frame.Failure} " +
                $"target={claw.CurrentTarget?.GetPath()} actual_anchor={frame.AnchorPoint} effective_visibility={frame.EffectiveVisibility} " +
                $"rope={frame.RopeLength:0.0} correction={frame.VelocityCorrection} " +
                $"instruction_targets=[{string.Join(",", instruction.Targets)}] context={instruction.ContextTitle} step={instruction.StepText}");
        }
        void CheckGuidance(string phase, params string[] requiredGlyphs)
        {
            if (!_assertFirstClawGuidance) return;
            var instruction = level.GetNode<PrologueGuidanceController>("Guidance").CurrentInstruction;
            var visual = level.GetNode<WorldTargetMarkerVisual>("Hud/Guidance/VisualRoot");
            Label label = visual.GetNode<Label>("Text");
            string targets = string.Join(",", instruction.Targets);
            bool correctTarget = instruction.Targets.Count > 0 &&
                !targets.Contains("Ice", StringComparison.OrdinalIgnoreCase) && visual.RenderedTargetCount == 1;
            bool complete = instruction.Visible && visual.IsVisibleInTree() && visual.HasVisiblePrompt &&
                label.IsVisibleInTree() && !string.IsNullOrWhiteSpace(instruction.StepText) &&
                label.Text.Contains(instruction.StepText, StringComparison.Ordinal) &&
                visual.PromptRect.Grow(.5f).Encloses(label.GetRect()) && label.GetMinimumSize().Y <= label.Size.Y + .5f;
            bool glyphsPresent = Array.TrueForAll(requiredGlyphs,
                glyph => instruction.StepText.Contains(glyph, StringComparison.Ordinal));
            bool obsoleteAbsent = phase != "latch" ||
                !instruction.StepText.Contains("W/S", StringComparison.Ordinal) &&
                !instruction.StepText.Contains("release LMB", StringComparison.OrdinalIgnoreCase);
            GD.Print($"DESCENT_FIRST_CLAW_GUIDANCE phase={phase} state={claw.CurrentFrame.State} targets=[{targets}] " +
                $"rendered_targets={visual.RenderedTargetCount} complete={complete} required_glyphs={string.Join(",", requiredGlyphs)} " +
                $"glyphs_present={glyphsPresent} prompt={visual.PromptRect} step={instruction.StepText}");
            if (!correctTarget || !complete || !glyphsPresent || !obsoleteAbsent)
                failures.Add($"first claw {phase} guidance must show one non-Ice target and complete {string.Join("/", requiredGlyphs)} controls; " +
                    $"targets=[{targets}] rendered={visual.RenderedTargetCount} complete={complete} glyphs={glyphsPresent}");
        }
        claw.ClawFrameChanged += OnClaw;
        try
        {
            await ToSignal(this, SignalName.ProcessFrame);
            AimFirstClaw(player, cursor);
            for (int i = 0; i < 3; i++) await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            MechanicalClawTargetResolution preflight = new MechanicalClawTargetResolver().Resolve(player, cursor);
            GD.Print($"DESCENT_FIRST_CLAW_PREFLIGHT surface={preflight.Surface?.GetPath()} " +
                $"snapshot={preflight.Snapshot} intended_hit={intendedHit} cursor={cursor}");
            Log("pre");
            CheckGuidance("pre", "E", "LMB");
            capture("descent-first-claw-pre");
            Vector2 screen = player.GetGlobalTransformWithCanvas() * player.ToLocal(cursor);
            if (!player.GetViewportRect().HasPoint(screen) || player.GlobalPosition.DistanceTo(intendedHit) > 620f)
            { failures.Add("first claw candidate is not visible/in range from the actual first landing"); return; }
            await ToSignal(this, SignalName.ProcessFrame);
            SendFirstClawMouse(player, cursor, true);
            await ToSignal(this, SignalName.PhysicsFrame);
            SendFirstClawMouse(player, cursor, false);
            ulong start = Engine.GetPhysicsFrames();
            bool attached = false;
            for (int sample = 0; sample < 360 && Engine.GetPhysicsFrames() - start < 120; sample++)
            {
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                if (sample % 12 == 0) Log("await-latch");
                attached = claw.CurrentTarget == target && claw.CurrentFrame.State is
                    MechanicalClawState.Latched or MechanicalClawState.Reel or MechanicalClawState.Swing;
                if (attached) break;
            }
            Log("latch-result");
            CheckGuidance("latch", "A/D", "SPACE");
            capture("descent-first-claw-latch");
            if (!attached || latches != 1 || wrongTarget)
            { failures.Add("first claw real LMB cast did not latch the intended FrostVentCeiling underside once"); return; }
            if (Math.Abs(claw.CurrentFrame.AnchorPoint.Y - 516f) > 1f ||
                Math.Abs(claw.CurrentFrame.AnchorPoint.X - 1950f) > 8f)
                failures.Add("first claw locked anchor is not the expected nearby underside collision hit");

            await ToSignal(this, SignalName.ProcessFrame);
            Send(InputActions.MoveRight, true);
            start = Engine.GetPhysicsFrames();
            bool released = false;
            int stable = 0;
            ulong lastPhysics = ulong.MaxValue;
            for (int sample = 0; sample < 900 && Engine.GetPhysicsFrames() - start < 360 && stable < 12; sample++)
            {
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                if (sample % 12 == 0) Log(released ? "released-flight" : "D-auto-pull");
                // Once above the safe interior, remove all steering and let real release physics settle.
                if (!released && player.GlobalPosition.X >= 1900f && player.GlobalPosition.Y <= 680f)
                {
                    Log("release-request");
                    await ToSignal(this, SignalName.ProcessFrame);
                    Send(InputActions.MoveRight, false);
                    SendFirstClawMouse(player, cursor, true);
                    await ToSignal(this, SignalName.PhysicsFrame);
                    SendFirstClawMouse(player, cursor, false);
                    released = true;
                }
                ulong physics = Engine.GetPhysicsFrames();
                if (physics != lastPhysics)
                {
                    stable = released && HasUpwardContact(player, landing) &&
                        player.GlobalPosition.X >= 1870f && player.GlobalPosition.X <= 2130f ? stable + 1 : 0;
                    lastPhysics = physics;
                }
            }
            Log("landing-result");
            capture("descent-first-claw-landing");
            GD.Print($"DESCENT_FIRST_CLAW_SUMMARY casts={casts} latches={latches} releases={releases} " +
                $"wrong_target={wrongTarget} boost={boostSeen} stable_physics={stable} support={landing.GetPath()} player={player.GlobalPosition}");
            if (casts != 1 || latches != 1 || releases != 1 || wrongTarget || boostSeen || stable < 12)
                failures.Add("first claw must use one real underside cast/latch/release and stabilize on GripLanding for twelve physics ticks without Boost");
            if (player.GetNode<PlayerMotor>("Motor").AcceptedJumpCount != jumps)
                failures.Add("first claw demonstration unexpectedly added an ordinary jump");
        }
        finally
        {
            claw.ClawFrameChanged -= OnClaw;
            await ToSignal(this, SignalName.ProcessFrame);
            Send(InputActions.MoveRight, false);
            SendFirstClawMouse(player, cursor, false);
        }
    }

    private static Vector2 AimFirstClaw(CharacterBody2D player, Vector2 cursor)
    {
        Vector2 pointer = player.GetViewport().GetFinalTransform() *
            (player.GetGlobalTransformWithCanvas() * player.ToLocal(cursor));
        var motion = new InputEventMouseMotion { Position = pointer, GlobalPosition = pointer };
        player.GetViewport().PushInput(motion, true);
        Input.ParseInputEvent(motion);
        Input.FlushBufferedEvents();
        return pointer;
    }

    private static void SendFirstClawMouse(CharacterBody2D player, Vector2 cursor, bool pressed)
    {
        Vector2 pointer = AimFirstClaw(player, cursor);
        Input.ParseInputEvent(new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = pressed,
            Position = pointer, GlobalPosition = pointer, ButtonMask = pressed ? MouseButtonMask.Left : (MouseButtonMask)0 });
        Input.FlushBufferedEvents();
    }

    private static bool HasUpwardContact(CharacterBody2D player, StaticBody2D support)
    {
        if (!player.IsOnFloor()) return false;
        for (int i = 0; i < player.GetSlideCollisionCount(); i++)
        {
            KinematicCollision2D collision = player.GetSlideCollision(i);
            if (collision.GetCollider() == support && collision.GetNormal().Y < -.7f) return true;
        }
        return false;
    }

    private static Rect2 ProjectPlatformTop(StaticBody2D support)
    {
        CollisionShape2D collision = support.GetNode<CollisionShape2D>("Collision");
        if (collision.Shape is not RectangleShape2D rectangle)
            throw new InvalidOperationException("Descent support must have its authored rectangular collision.");
        Vector2 half = rectangle.Size * .5f;
        Transform2D canvas = collision.GetGlobalTransformWithCanvas();
        Vector2 first = canvas * new Vector2(-half.X, -half.Y - 4f);
        Rect2 band = new(first, Vector2.Zero);
        foreach (Vector2 corner in new[] { new Vector2(half.X, -half.Y - 4f),
                     new Vector2(half.X, -half.Y + 4f), new Vector2(-half.X, -half.Y + 4f) })
            band = band.Expand(canvas * corner);
        return band;
    }

    private void ObserveExitGuidance(PrologueLevelController level, CharacterBody2D player,
        string stage, string output, PrologueSectorId expectedSector, string targetPath, List<string> failures)
    {
        var instruction = level.GetNode<PrologueGuidanceController>("Guidance").CurrentInstruction;
        var visual = level.GetNode<WorldTargetMarkerVisual>("Hud/Guidance/VisualRoot");
        var objective = level.GetNode<PrologueCompletionController>("Completion").CurrentObjective;
        Node2D expectedTarget = level.CurrentSectorRoot.GetNodeOrNull<Node2D>(targetPath);
        bool resolvedExpected = false;
        var paths = new List<string>();
        foreach (PrologueGuidanceTargetKey key in instruction.Targets)
        {
            bool resolved = PrologueGuidanceTargetResolver.TryResolve(level.CurrentSectorRoot,
                level.CurrentSector.Value, key, out Node2D target);
            paths.Add(key + "=" + (resolved ? target.GetPath().ToString() : "UNRESOLVED"));
            resolvedExpected |= resolved && target == expectedTarget && target.IsInsideTree();
        }
        GD.Print($"FIRST_ENEMY_EXIT_GUIDANCE stage={stage} physics={Engine.GetPhysicsFrames()} process={Engine.GetProcessFrames()} " +
            $"player={player.GlobalPosition} objective={objective} sector={level.CurrentSector} sector_path={level.CurrentSectorRoot.GetPath()} " +
            $"targets=[{string.Join(",", paths)}] rendered_targets={visual.RenderedTargetCount} " +
            $"expected_path={expectedTarget?.GetPath()} expected_position={expectedTarget?.GlobalPosition} " +
            $"step={instruction.StepText} ribbon={level.GetNode<Label>("Hud/FormalHud/ObjectiveRibbon/Text").Text}");
        if (output != null)
        {
            using Image image = Root.GetTexture().GetImage();
            if (image == null || image.IsEmpty() || image.SavePng(Path.Combine(output, stage + ".png")) != Error.Ok)
                throw new InvalidOperationException("Could not capture " + stage);
        }
        if (level.CurrentSector != expectedSector) failures.Add(stage + ": actual input did not reach expected sector " + expectedSector);
        if (objective != PrologueObjective.ReachBasin) failures.Add(stage + ": actual E must leave objective ReachBasin");
        if (!instruction.Visible || !visual.IsVisibleInTree() || !string.IsNullOrEmpty(instruction.ContextTitle))
            failures.Add(stage + ": ordinary traversal guidance must replace the combat context");
        if (!resolvedExpected || visual.RenderedTargetCount < 1)
            failures.Add(stage + ": traversal card must render its live, resolved forward target " + targetPath);
        if (expectedSector == PrologueSectorId.CanyonEntrance &&
            GodotObject.IsInstanceValid(expectedTarget) && expectedTarget.GlobalPosition.X <= player.GlobalPosition.X)
            failures.Add(stage + ": entrance exit marker must point ahead of the player");
    }

    // Opt-in input script, not a claim of human comprehension. No model/guard calls:
    // success requires real collision-driven guard results and counter damage events.
    private sealed class DefendCounterObserver : IDisposable
    {
        private readonly PrologueLevelController _level;
        private readonly CharacterBody2D _player;
        private readonly RustShellSquire _enemy;
        private readonly PlayerShieldController _shield;
        private readonly PlayerEnergyController _energy;
        private readonly PlayerActionModeController _modes;
        private readonly PlayerCombat _combat;
        private readonly PlayerInputReader _input;
        private readonly HealthComponent _health, _enemyHealth;
        private readonly string _output;
        private readonly bool _sameTickCounter;
        private readonly bool _assertCombatGuidance;
        private readonly HashSet<string> _guidanceStages = new();
        private readonly List<string> _guidanceFailures = new();
        private readonly int _initialHealth, _initialEnemyHealth;
        private readonly HashSet<string> _screenshots = new();
        private ulong _attackId, _telegraphTick, _qTick, _shieldReleaseTick, _primaryTick;
        private bool _selected, _qHeld, _shieldHeld, _releasePending, _primaryHeld, _counterIssued;
        private int _rounds, _guards, _counterRequests, _acceptedCounters, _counterDamage;
        public bool Done => _rounds > 2 || _counterRequests > 0 && !_primaryHeld && !_combat.IsAttacking &&
            Elapsed(_primaryTick) >= .4f && (_counterDamage > 0 || _counterRequests >= 2);

        public DefendCounterObserver(PrologueLevelController level, CharacterBody2D player,
            RustShellSquire enemy, string output, bool sameTickCounter, bool assertCombatGuidance)
        {
            _level = level; _player = player; _enemy = enemy; _output = output; _sameTickCounter = sameTickCounter;
            _assertCombatGuidance = assertCombatGuidance;
            _shield = player.GetNode<PlayerShieldController>("Shield");
            _energy = player.GetNode<PlayerEnergyController>("Energy");
            _modes = player.GetNode<PlayerActionModeController>("ActionModes");
            _combat = player.GetNode<PlayerCombat>("Combat");
            _input = player.GetNode<PlayerInputReader>("Input");
            _health = player.GetNode<HealthComponent>("Health");
            _enemyHealth = enemy.GetNode<HealthComponent>("Health");
            _initialHealth = _health.CurrentHealth; _initialEnemyHealth = _enemyHealth.CurrentHealth;
            _shield.ShieldResolved += OnGuard;
            _enemyHealth.Damaged += OnEnemyDamage;
            _combat.AttackStarted += OnCounterStarted;
            _modes.PrimaryRejected += OnCounterRejected;
            Log($"begin; same_tick_counter={_sameTickCounter}; scripted minimum reaction=0.25s, maximum two rounds; not a human comprehension result");
        }

        public void ObserveAttack(AttackTimelineFrame frame)
        {
            if (frame.Phase != AttackPhase.Telegraph || frame.AttackId == _attackId) return;
            _attackId = frame.AttackId; _telegraphTick = Engine.GetPhysicsFrames();
            _rounds++; _counterIssued = false;
        }

        public void Tick(AttackTimelineFrame frame, bool sameFloor, float distance)
        {
            ulong tick = Engine.GetPhysicsFrames();
            Vector2 pointer = _player.GetViewport().GetFinalTransform() *
                (_player.GetGlobalTransformWithCanvas() * _player.ToLocal(_enemy.GlobalPosition));
            var motion = new InputEventMouseMotion { Position = pointer, GlobalPosition = pointer };
            _player.GetViewport().PushInput(motion, true);
            Input.ParseInputEvent(motion);
            Input.FlushBufferedEvents();
            if (!_selected && sameFloor && Math.Abs(distance) < 360f)
            {
                Send(InputActions.SelectAttackMode, true);
                _selected = _qHeld = true; _qTick = tick;
                Log("Q press while approaching");
            }
            if (_qHeld && tick > _qTick) { Send(InputActions.SelectAttackMode, false); _qHeld = false; }
            if (_primaryHeld && tick - _primaryTick >= 2) { Send(InputActions.PrimaryAction, false); _primaryHeld = false; }
            if (_rounds is < 1 or > 2) return;
            if (frame.Phase == AttackPhase.Telegraph)
            {
                CheckCombatGuidance("telegraph");
                Capture($"defend-{_rounds}-telegraph");
                if (!_shieldHeld && Elapsed(_telegraphTick) >= .25f)
                {
                    Send(InputActions.Shield, true); _shieldHeld = true;
                    Log($"RMB press reaction_seconds={Elapsed(_telegraphTick):0.000} remaining={frame.PhaseRemainingSeconds:0.000}");
                }
            }
            if (_shield.IsActive) Capture($"defend-{_rounds}-shield-up");
            if (frame.Phase == AttackPhase.Recovery && _shieldHeld)
            {
                Send(InputActions.Shield, false); _shieldHeld = false;
                _releasePending = true; _shieldReleaseTick = tick;
                Log("RMB release during Recovery");
                if (_sameTickCounter)
                    RequestCounter(tick, "same postdraw: released RMB then newly pressed LMB; no earlier held-primary edge");
            }
            if (_releasePending && tick > _shieldReleaseTick && !_shield.IsActive && !_counterIssued &&
                frame.Phase == AttackPhase.Recovery)
            {
                RequestCounter(tick, $"confirmed shield-down automatic_ticks={tick - _shieldReleaseTick} recovery_remaining={frame.PhaseRemainingSeconds:0.000}");
            }
            if (_counterIssued && _combat.IsAttacking) Capture($"defend-{_rounds}-counter");
        }

        private void RequestCounter(ulong tick, string reason)
        {
            _releasePending = false; _counterIssued = true;
            Send(InputActions.PrimaryAction, true); _primaryHeld = true; _primaryTick = tick; _counterRequests++;
            Log("LMB counter press: " + reason);
        }
        private void OnCounterRejected(PlayerActionRejectionReason reason) => Log("actual_primary_rejected=" + reason);
        private void OnGuard(PlayerShieldResult result, ulong transition)
        {
            CheckCombatGuidance("guard");
            if (result.Guarded && result.DamageAmount == 0) _guards++;
            Log($"guard_result={result} transition={transition}");
        }
        private void OnCounterStarted(ulong id, int direction)
        {
            CheckCombatGuidance("counter");
            if (_counterIssued) _acceptedCounters++;
            Log($"actual_counter_started id={id} direction={direction}");
        }
        private void OnEnemyDamage(int remaining, Vector2 impulse)
        {
            if (_counterIssued && _acceptedCounters > 0) _counterDamage++;
            Log($"enemy_damaged remaining={remaining} impulse={impulse}");
        }
        private static float Elapsed(ulong start) =>
            (Engine.GetPhysicsFrames() - start) / (float)Engine.PhysicsTicksPerSecond;
        private void Log(string stage)
        {
            PrologueGuidanceInstruction guidance = _level.GetNode<PrologueGuidanceController>("Guidance").CurrentInstruction;
            PrologueHudViewModel hud = _level.GetNode<PrologueFormalHud>("Hud/FormalHud").CurrentModel;
            GD.Print($"FIRST_ENEMY_DEFEND physics={Engine.GetPhysicsFrames()} process={Engine.GetProcessFrames()} stage={stage} " +
                $"round={_rounds} player={_player.GlobalPosition} enemy={_enemy.GlobalPosition} hp={_health.CurrentHealth} enemy_hp={_enemyHealth.CurrentHealth} " +
                $"energy={_energy.EnergyRatio:0.000} heat={_energy.HeatRatio:0.000} shield={_shield.IsActive} mode={_modes.CurrentMode} rejection={_modes.LastRejection} " +
                $"input_sequence={_input.CurrentFrameSequence} sampled_shield={_input.Current.ShieldHeld} sampled_primary_pressed={_input.Current.PrimaryPressed} " +
                $"guidance_visible={guidance.Visible} guidance={guidance.StepText} hint={guidance.HintText} hud_prompt={hud.PromptText}");
        }
        private void Capture(string name)
        {
            if (_output == null || !_screenshots.Add(name)) return;
            using Image image = _player.GetViewport().GetTexture().GetImage();
            if (image == null || image.IsEmpty() || image.SavePng(Path.Combine(_output, name + ".png")) != Error.Ok)
                throw new InvalidOperationException("Could not capture " + name);
            Log("captured " + name);
        }
        public void AssertOutcome(int playerHits)
        {
            Log($"summary guards={_guards} counter_requests={_counterRequests} accepted_counters={_acceptedCounters} enemy_damage_events={_counterDamage} player_hits={playerHits}");
            if (_guards < 1 || playerHits != 0 || _health.CurrentHealth != _initialHealth)
                throw new InvalidOperationException("Defend/counter requires a real zero-damage guard and no player damage.");
            if (_acceptedCounters < 1 || _counterDamage < 1 || _enemyHealth.CurrentHealth >= _initialEnemyHealth)
                throw new InvalidOperationException("Defend/counter never caused real enemy damage with an accepted counterattack.");
            if (_assertCombatGuidance && (_guidanceStages.Count != 3 || _guidanceFailures.Count > 0))
                throw new InvalidOperationException("Combat guidance stages=" + string.Join(",", _guidanceStages) +
                    "; failures=" + string.Join(" | ", _guidanceFailures));
        }
        private void CheckCombatGuidance(string stage)
        {
            if (!_assertCombatGuidance || !_guidanceStages.Add(stage)) return;
            // Record failures instead of throwing from combat callbacks: observation
            // must not interrupt real guard resolution or attack dispatch.
            try { FirstCombatGuidanceRuntimeTests.AssertCombatGuidance(_level, "real defend " + stage); }
            catch (InvalidOperationException error) { _guidanceFailures.Add(stage + ": " + error.Message); }
        }
        public void Dispose()
        {
            _shield.ShieldResolved -= OnGuard;
            _enemyHealth.Damaged -= OnEnemyDamage;
            _combat.AttackStarted -= OnCounterStarted;
            _modes.PrimaryRejected -= OnCounterRejected;
        }
    }

    private static float BeyondRectangle(Vector2 point, Vector2 origin, Vector2 extent, float rotation)
    {
        Vector2 local = (point - origin).Rotated(-rotation);
        return new Vector2(Mathf.Max(0, Mathf.Abs(local.X) - extent.X),
            Mathf.Max(0, Mathf.Abs(local.Y) - extent.Y)).Length();
    }

    private static void Send(StringName action, bool pressed)
    {
        Input.ParseInputEvent(new InputEventAction { Action = action, Pressed = pressed, Strength = pressed ? 1f : 0f });
        Input.FlushBufferedEvents();
    }

    private static void ReleaseAll()
    {
        foreach (StringName action in new[] { InputActions.MoveLeft, InputActions.MoveRight, InputActions.MoveUp,
            InputActions.MoveDown, InputActions.Jump, InputActions.Dash, InputActions.PrimaryAction,
            InputActions.Shield, InputActions.SelectAttackMode, InputActions.SelectGrappleMode,
            InputActions.Slot1, InputActions.Slot2, InputActions.Slot3, InputActions.Slot4 }) Send(action, false);
    }
}
