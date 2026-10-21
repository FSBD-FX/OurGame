using System;
using System.IO;
using System.Threading.Tasks;
using Godot;
using Knight.Abilities;
using Knight.Boss;
using Knight.Combat;
using Knight.Player;
using Knight.Presentation.Fog;
using Knight.Prologue;
using Knight.Save;

namespace Knight.Tests.Integration;

/// <summary>Actual saved entity with controlled metadata, NOT a natural collection/backtrack route.</summary>
public static class BossAdmissionRuntimeTests
{
    private static readonly string[] Fragments =
    [
        "canyon.v2.fragment.entrance", "canyon.v2.fragment.rift",
        "canyon.v2.fragment.abyss-floor", "canyon.v2.fragment.gear-gallery",
        "canyon.v2.fragment.ascent-mid", "canyon.v2.fragment.courtyard"
    ];

    public static async Task Run(SceneTree tree)
    {
        var level = GD.Load<PackedScene>("res://scenes/prologue/PrologueLevel.tscn")
            .Instantiate<PrologueLevelController>();
        try
        {
            var paths = IsolatedPrologueFixture.Configure(level, "boss-admission-entity");
            var flow = level.GetNode<PrologueGameFlowController>("GameFlow");
            flow.HeadlessAutoStart = false; flow.AutomationAutoStart = false;
            tree.Root.AddChild(level);
            await Frames(tree, 5);
            flow.StartNew();
            await Frames(tree, 5);
            level.RestoreFromSave(Snapshot(5, true, true));
            await Frames(tree, 8);
            var arena = level.CurrentSectorRoot.GetNode<BossArenaController>("Gameplay/BossArenaController");
            var entry = level.CurrentSectorRoot.GetNode<CollisionShape2D>("Gameplay/Doors/Entry/Collision");
            TestAssert.True(level.Progress.CollectedFragmentIds.Count == 5 &&
                level.Progress.BellChainShortcutOpen &&
                level.Progress.ActivatedMechanisms.Contains("canyon.v2.courtyard-seal-open") &&
                !level.Progress.LegacyCollectionQuotaExempt && !arena.IsEncounterActive,
                "five-fragment current save has both original prerequisites and a dormant actual Boss");
            TestAssert.True(!entry.Disabled,
                "saved final Boss entry stays physically closed with five fragments despite both original prerequisites");
            var barrier = level.CurrentSectorRoot.GetNode<CollisionShape2D>("Gameplay/Doors/AdmissionBarrier/Collision");
            var exit = level.CurrentSectorRoot.GetNode<CollisionShape2D>("Gameplay/Doors/Exit/Collision");
            using (var roofProbe = PhysicsRayQueryParameters2D.Create(new Vector2(7200, 700),
                new Vector2(7200, 800), CombatCollisionLayers.World))
            {
                var roofHit = level.Player.GetWorld2D().DirectSpaceState.IntersectRay(roofProbe);
                TestAssert.True(roofHit.Count > 0 && roofHit["collider"].AsGodotObject() == barrier.GetParent(),
                    "closed admission has an actual native roof away from the old ceiling beam, not an unbounded-height pillar assumption");
            }
            var top = barrier.GetParent().GetNode<CollisionShape2D>("TopCollision");
            var right = barrier.GetParent().GetNode<CollisionShape2D>("RightCollision");
            var admissionBody = barrier.GetParent<StaticBody2D>();
            var hull = new[] { barrier, top, right };
            bool HullDisabled(bool disabled) => Array.TrueForAll(hull, shape => shape.Disabled == disabled);
            TestAssert.True(arena.RequiresFragmentAdmission && HullDisabled(false) && !exit.Disabled &&
                admissionBody.CollisionLayer == 33 && admissionBody.CollisionMask == 0 &&
                admissionBody.GetChildCount() == 4,
                "saved current arena opts in and owns all three native pre-fight shapes on one forbidden world body");
            foreach (var (shape, center, size) in new[]
            {
                (barrier, new Vector2(6160, 938), new Vector2(36, 436)),
                (top, new Vector2(6920, 740), new Vector2(1556, 40)),
                (right, new Vector2(7680, 938), new Vector2(36, 436))
            })
                TestAssert.True(shape.Shape is RectangleShape2D rectangle && rectangle.Size == size &&
                    shape.GlobalPosition == center && shape.GlobalScale == Vector2.One &&
                    shape.GlobalRotation == 0 && !shape.OneWayCollision,
                    "builder roundtrip preserves the independently specified finite closed admission rectangles");
            var arenaFloor = level.CurrentSectorRoot.GetNode<CollisionShape2D>("Gameplay/Collision/ArenaFloor/Collision");
            TestAssert.True(Rect(barrier).Intersection(Rect(top)).Size == new Vector2(36, 40) &&
                Rect(right).Intersection(Rect(top)).Size == new Vector2(36, 40) &&
                Rect(barrier).Intersection(Rect(arenaFloor)).Size == new Vector2(36, 40) &&
                Rect(right).Intersection(Rect(arenaFloor)).Size == new Vector2(18, 40),
                "four admission corners have positive rectangle overlaps, including the unchanged floor");
            foreach (float y in new[] { 800f, 900f })
                AssertRayShape(level, new(6100, y), new(6200, y), barrier);
            foreach (float x in new[] { 6400f, 7000f, 7600f })
                AssertRayShape(level, new(x, 700), new(x, 800), top);
            foreach (float y in new[] { 800f, 1000f, 1130f })
                AssertRayShape(level, new(7720, y), new(7640, y), right);
            AssertRayShape(level, new(7000, 1180), new(7000, 1090), arenaFloor);
            foreach (var (position, first, second) in new[]
            {
                (new Vector2(6160, 740), barrier, top),
                (new Vector2(7680, 740), right, top),
                (new Vector2(6160, 1136), barrier, arenaFloor),
                (new Vector2(7680, 1136), right, arenaFloor)
            })
                AssertCapsuleOverlap(level, position, first, second);
            using var occlusion = new HeadlampOcclusionSampler(level.GetNode<Node2D>("WorldRoot"));
            void AssertHullOcclusion(bool present)
            {
                var frame = occlusion.Capture();
                TestAssert.True(frame.IsComplete && frame.Shapes.Count <= HeadlampOcclusionSampler.MaximumShapes,
                    "finite hull remains within complete native fog geometry capacity");
                foreach (CollisionShape2D shape in hull)
                {
                    bool found = false;
                    foreach (HeadlampOccluder item in frame.Shapes)
                        if (item.WorldToLocal == shape.GlobalTransform.AffineInverse() &&
                            item.LocalBounds == new Rect2(-((RectangleShape2D)shape.Shape).Size / 2,
                                ((RectangleShape2D)shape.Shape).Size)) found = true;
                    TestAssert.True(found == present, "each admission shape joins/leaves native fog geometry with collision state");
                }
            }
            AssertHullOcclusion(true);
            GD.Print("BOSS_ADMISSION_FINITE_HULL_GEOMETRY_OK shapes=3 corners=4 native_shape_owners=true capsule_queries=true");
            var panel = entry.GetParent().GetNode<CanvasItem>("ReadableDoor");
            TestAssert.True(panel.Visible, "insufficient quota retains the visible physical door panel");
            TestAssert.True(level.Player.GetNode<CollisionShape2D>("BodyCollider").Shape is CapsuleShape2D capsule &&
                capsule.Radius == 20 && capsule.Height == 72,
                "independent body-bound assertions use the saved real radius20 height72 capsule");
            foreach (var (name, center, size) in new[]
            {
                ("CheckpointFloor", new Vector2(5880, 1136), new Vector2(400, 40)),
                ("ArenaFloor", new Vector2(6880, 1136), new Vector2(1600, 40))
            })
            {
                var shape = level.CurrentSectorRoot.GetNode<CollisionShape2D>($"Gameplay/Collision/{name}/Collision");
                TestAssert.True(!shape.Disabled && shape.GlobalPosition == center &&
                    shape.Shape is RectangleShape2D floor && floor.Size == size,
                    "actual two adjacent native floors form one continuous full-capsule support interval5680..7680 at1116");
            }
            arena.BeginEncounter();
            TestAssert.True(!arena.IsEncounterActive, "direct public activation also rejects five fragments");

            Place(level, new Vector2(6120, 1080));
            await Frames(tree, 5);
            int health = level.Player.GetNode<HealthComponent>("Health").CurrentHealth;
            int recoveries = 0;
            level.RecoveryCompleted += _ => recoveries++;
            bool contact = false;
            KeyEvent(Key.D, true);
            try
            {
                for (int tick = 0; tick < 36; tick++)
                {
                    await Frames(tree, 1);
                    TestAssert.True(level.Player.GlobalPosition.X + 20 <= 6142.5f &&
                        !arena.IsEncounterActive && !exit.Disabled, "native D cannot cross the closed door into Boss");
                    for (int index = 0; index < level.Player.GetSlideCollisionCount(); index++)
                        if (level.Player.GetSlideCollision(index).GetCollider() == barrier.GetParent() ||
                            level.Player.GetSlideCollision(index).GetCollider() == entry.GetParent()) contact = true;
                }
            }
            finally { KeyEvent(Key.D, false); }
            GD.Print($"BOSS_ADMISSION_REFUSAL_OBSERVATION position={level.Player.GlobalPosition} velocity={level.Player.Velocity} " +
                $"floor={level.Player.IsOnFloor()} support={Supported(level)} contact={contact} recoveries={recoveries} " +
                $"health_before={health} health_after={level.Player.GetNode<HealthComponent>("Health").CurrentHealth} flow={flow.State} " +
                $"motor={level.Player.GetNode<PlayerMotor>("Motor").IsPhysicsProcessing()} entry_disabled={entry.Disabled} barrier_disabled={barrier.Disabled}");
            for (int index = 0; index < level.Player.GetSlideCollisionCount(); index++)
            {
                var slide = level.Player.GetSlideCollision(index);
                GD.Print($"BOSS_ADMISSION_CONTACT collider={(slide.GetCollider() as Node)?.GetPath()} normal={slide.GetNormal()}");
            }
            TestAssert.True(contact && Supported(level) && recoveries == 0 &&
                level.Player.GetNode<HealthComponent>("Health").CurrentHealth == health,
                "five-fragment physical refusal includes native gate contact, full real floor support and no damage/recovery");
            KeyEvent(Key.A, true);
            await Frames(tree, 12);
            KeyEvent(Key.A, false);
            await Frames(tree, 5);
            GD.Print($"BOSS_ADMISSION_RETREAT_OBSERVATION position={level.Player.GlobalPosition} support={Supported(level)} active={arena.IsEncounterActive}");
            TestAssert.True(level.Player.GlobalPosition.X < 6095 && Supported(level) && !arena.IsEncounterActive,
                "player can really retreat left from the admission door without falling or combat");
            GD.Print("BOSS_ADMISSION_NATIVE_FIVE_REFUSAL_OK direct=true contact=true supported=true retreat=true");

            Place(level, new Vector2(6120, 1080));
            await Frames(tree, 5);
            var motor = level.Player.GetNode<PlayerMotor>("Motor");
            int jumpsBefore = motor.AcceptedJumpCount;
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            KeyEvent(Key.D, true); KeyEvent(Key.Space, true);
            for (int tick = 0; tick < 14; tick++)
            {
                await Frames(tree, 1);
                TestAssert.True(level.Player.GlobalPosition.X + 20 <= 6142.5f && !arena.IsEncounterActive,
                    "real grounded ordinary jump cannot cross the closed admission left wall");
            }
            ReleaseKeys();
            TestAssert.True(motor.AcceptedJumpCount == jumpsBefore + 1 && !level.Player.IsOnFloor(),
                "negative genuinely accepted exactly one ordinary jump, not only a held jump key");

            // Explicit high-pose negative, not proof that every route to this pose is reachable.
            // It is above the old Entry's roof and exercises the actual keyboard K path.
            Place(level, new Vector2(6120, 800));
            await Frames(tree, 2);
            var claw = level.Player.GetNode<MechanicalClawController>("MechanicalClaw");
            bool rejectedNoTarget = false;
            void Observe(MechanicalClawFrame frame, GrappleSurface2D target)
            { if (frame.Failure == MechanicalClawFailure.NoTarget) rejectedNoTarget = true; }
            TestAssert.True(barrier.GetParent().GetNode<GrappleSurface2D>("GrappleSurfaceProfile").Material ==
                GrappleSurfaceMaterial.Forbidden, "admission collision's authored material is explicitly forbidden");
            claw.ClawFrameChanged += Observe;
            KeyEvent(Key.D, true); KeyEvent(Key.S, true);
            await Frames(tree, 2);
            KeyEvent(Key.K, true);
            try
            {
                // First observe the ordinary K edge without dash suppressing the
                // cast; then separately challenge the collision with dash/jump.
                for (int tick = 0; tick < 6; tick++)
                {
                    await Frames(tree, 1);
                    TestAssert.True(level.Player.GlobalPosition.X + 20 <= 6142.5f &&
                        !arena.IsEncounterActive && claw.CurrentTarget == null,
                        "native K cannot acquire a target behind admission barrier");
                }
                KeyEvent(Key.K, false); KeyEvent(Key.S, false);
                await Frames(tree, 1);
                int dashesBefore = motor.AcceptedDashCount;
                KeyEvent(Key.Shift, true); KeyEvent(Key.Space, true);
                for (int tick = 0; tick < 8; tick++)
                {
                    await Frames(tree, 1);
                    TestAssert.True(level.Player.GlobalPosition.X + 20 <= 6142.5f && !arena.IsEncounterActive,
                        "actual air dash with simultaneous jump input cannot bypass the original door roof through admission barrier");
                }
                TestAssert.True(motor.AcceptedDashCount == dashesBefore + 1,
                    "high-pose negative genuinely accepts one actual air dash");
            }
            finally
            {
                ReleaseKeys(); claw.ClawFrameChanged -= Observe;
            }
            GD.Print($"BOSS_ADMISSION_ROOF_OBSERVATION position={level.Player.GlobalPosition} forbidden_material=true rejected_no_target={rejectedNoTarget} claw={claw.CurrentFrame.State} failure={claw.CurrentFrame.Failure}");
            // Keyboard selection deliberately excludes forbidden/ice candidates;
            // its existing public failure is NoTarget, not the pointer path's Forbidden.
            TestAssert.True(rejectedNoTarget && level.Player.GlobalPosition.Y + 36 < 936,
                "actual keyboard K finds no eligible target through the forbidden barrier while the whole capsule remains above the old door roof");
            GD.Print("BOSS_ADMISSION_NATIVE_ROOF_NEGATIVE_OK keyboard_k=true forbidden_material=true rejected_no_target=true all_reachable_routes=false");

            // These are controlled exterior poses, not claimed natural route arrivals.
            // Actual keyboard air dashes must collide with the specific roof/right shape.
            ReleaseKeys();
            Place(level, new Vector2(6800, 620));
            await Frames(tree, 2);
            rejectedNoTarget = false;
            claw.ClawFrameChanged += Observe;
            KeyEvent(Key.S, true);
            await Frames(tree, 2);
            KeyEvent(Key.K, true);
            await Frames(tree, 6);
            KeyEvent(Key.K, false);
            claw.ClawFrameChanged -= Observe;
            TestAssert.True(rejectedNoTarget && claw.CurrentTarget == null,
                "actual downward K cannot acquire the old arena beam through the finite forbidden roof");
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            int roofDashes = motor.AcceptedDashCount;
            bool roofContact = false;
            KeyEvent(Key.Shift, true);
            for (int tick = 0; tick < 12; tick++)
            {
                await Frames(tree, 1);
                roofContact |= ContactShape(level.Player, top);
                TestAssert.True(level.Player.GlobalPosition.Y + 36 <= 720.5f && !arena.IsEncounterActive && HullDisabled(false),
                    "actual downward air dash remains above the finite admission roof with insufficient quota");
            }
            ReleaseKeys();
            TestAssert.True(motor.AcceptedDashCount == roofDashes + 1 && roofContact && recoveries == 0 &&
                level.Player.GetNode<HealthComponent>("Health").CurrentHealth == health,
                "roof negative genuinely accepts one air dash and contacts its native roof shape without damage/recovery");
            GD.Print($"BOSS_ADMISSION_FINITE_TOP_INGRESS_OK controlled_pose=true dash=1 native_top=true position={level.Player.GlobalPosition}");

            Place(level, new Vector2(7730, 1000));
            await Frames(tree, 2);
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            int eastDashes = motor.AcceptedDashCount;
            bool eastContact = false;
            KeyEvent(Key.A, true); KeyEvent(Key.Shift, true);
            for (int tick = 0; tick < 8; tick++)
            {
                await Frames(tree, 1);
                eastContact |= ContactShape(level.Player, right);
                TestAssert.True(level.Player.GlobalPosition.X - 20 >= 7697.5f && !arena.IsEncounterActive && HullDisabled(false),
                    "actual westward air dash cannot enter around the finite right boundary");
            }
            ReleaseKeys();
            TestAssert.True(motor.AcceptedDashCount == eastDashes + 1 && eastContact && recoveries == 0 &&
                level.Player.GetNode<HealthComponent>("Health").CurrentHealth == health,
                "east negative genuinely accepts one air dash and contacts its native right shape without recovery/damage");
            GD.Print($"BOSS_ADMISSION_FINITE_EAST_INGRESS_OK controlled_pose=true dash=1 native_right=true position={level.Player.GlobalPosition}");

            // Controlled relocation to the wrong side proves trigger defense, not lawful traversal.
            Place(level, new Vector2(6260, 1080));
            await Frames(tree, 5);
            TestAssert.True(level.CurrentSectorRoot.GetNode<Area2D>("Gameplay/ActivationTrigger")
                .GetOverlappingBodies().Contains(level.Player) && !arena.IsEncounterActive,
                "actual BodyEntered cannot activate the insufficient-quota Boss from a controlled inside pose");

            async Task AssertSnapshot(PrologueSaveData saved, bool open, string label)
            {
                level.RestoreFromSave(saved);
                arena.ResetForCheckpoint();
                await Frames(tree, 8);
                TestAssert.True(entry.Disabled == open && HullDisabled(open) && !exit.Disabled &&
                    panel.Visible != open && !arena.IsEncounterActive, label + ": same physical AND gate and visible panel");
                AssertHullOcclusion(!open);
                if (!open)
                {
                    arena.BeginEncounter();
                    TestAssert.True(!arena.IsEncounterActive, label + ": public activation fails closed");
                }
            }
            await AssertSnapshot(Snapshot(5, true, true), false, "controlled dormant damage prerequisite");
            var dormantHealth = arena.CurrentBoss.GetNode<HealthComponent>("Health");
            int dormantDefeats = 0;
            arena.EncounterDefeated += () => dormantDefeats++;
            TestAssert.True(dormantHealth.ApplyDamage(new DamageRequest(dormantHealth.CurrentHealth,
                Vector2.Right, 0, 930001UL)), "controlled actual dormant Boss Health death is accepted by its own component");
            await Frames(tree, 5);
            TestAssert.True(dormantDefeats == 0 && !level.Progress.BossDefeated && !arena.IsEncounterActive &&
                !entry.Disabled && !exit.Disabled && HullDisabled(false),
                "unadmitted dormant Health death cannot emit victory or open any admission/combat gate");

            await AssertSnapshot(Snapshot(5, true, true), false, "same mounted quota update prerequisite");
            ulong originalBoss = arena.CurrentBoss.GetInstanceId();
            TestAssert.True(level.Progress.TryCollectFragment("canyon.v2.fragment.courtyard"),
                "controlled sixth unique fragment updates public durable state");
            await Frames(tree, 3);
            TestAssert.True(entry.Disabled && HullDisabled(true) && !exit.Disabled &&
                arena.CurrentBoss.GetInstanceId() == originalBoss && !arena.IsEncounterActive,
                "same mounted five-to-six state opens physical entry without reset/rebinding/new Boss");
            await AssertSnapshot(Snapshot(5, true, true), false, "same-frame sixth fragment activation prerequisite");
            originalBoss = arena.CurrentBoss.GetInstanceId();
            TestAssert.True(level.Progress.TryCollectFragment("canyon.v2.fragment.courtyard"),
                "same-frame prerequisite actually commits a sixth unique fragment");
            // Deliberately do not yield: activation must establish its own complete
            // door state instead of depending on the next pre-fight physics update.
            arena.BeginEncounter();
            await Frames(tree, 3);
            TestAssert.True(arena.IsEncounterActive && arena.CurrentBoss.GetInstanceId() == originalBoss &&
                !entry.Disabled && !exit.Disabled && HullDisabled(true),
                "same-frame sixth-fragment Begin closes combat doors but independently opens admission barrier");
            GD.Print("BOSS_ADMISSION_SAME_FRAME_SIXTH_BEGIN_OK physics_update_before_begin=false");
            AssertHullOcclusion(false);
            await AssertSnapshot(Snapshot(6, false, false), false, "same mounted mechanism update prerequisite");
            originalBoss = arena.CurrentBoss.GetInstanceId();
            level.Progress.ActivateMechanism("canyon.v2.courtyard-seal-open");
            await Frames(tree, 3);
            TestAssert.True(!entry.Disabled && HullDisabled(false), "same mounted seal alone does not replace missing bell");
            level.Progress.OpenBellChainShortcut();
            await Frames(tree, 3);
            TestAssert.True(entry.Disabled && HullDisabled(true) && arena.CurrentBoss.GetInstanceId() == originalBoss,
                "same mounted second original prerequisite updates AND gate without rebuilding Boss");
            await AssertSnapshot(Snapshot(6, false, true), false, "six but missing courtyard");
            await AssertSnapshot(Snapshot(6, true, false), false, "six but missing bell chain");
            await AssertSnapshot(Snapshot(0, true, true), false, "current schema forecourt is not legacy exempt");
            arena.BindRuntime(null, null);
            await Frames(tree, 2);
            TestAssert.True(!entry.Disabled && HullDisabled(false), "unbound opt-in arena fails closed");
            arena.BeginEncounter();
            TestAssert.True(!arena.IsEncounterActive, "unbound trigger cannot activate Boss");
            arena.BindRuntime(level.Progress, null);

            // Exercise real schema3 loading; never set the exemption field directly.
            string oldPath = Path.Combine(paths.CaseRoot, "eligible-schema3.json");
            File.WriteAllText(oldPath, """
                {"version":3,"layoutVersion":2,"checkpointId":"canyon.v2.temple-forecourt",
                 "hasActivatedCheckpoint":true,"checkpointSector":"LostTemple",
                 "checkpointWorldX":5920,"checkpointWorldY":1080,"openShortcuts":[],
                 "activatedMechanisms":[],"discoveries":[],"completedTutorials":[],
                 "shortcutOpen":false,"bossDefeated":false,"completed":false,"objective":"PullTempleMechanism"}
                """);
            var legacy = PrologueSaveStore.LoadOrNew(oldPath, 2);
            TestAssert.True(legacy.LegacyCollectionQuotaExempt && !legacy.WasRecovered &&
                legacy.CollectedFragmentIds.Count == 0, "actual legal schema3 migration creates quota-only exemption");
            await AssertSnapshot(legacy, false, "legacy quota exemption without original prerequisites");
            legacy.ActivatedMechanisms.Add("canyon.v2.courtyard-seal-open");
            await AssertSnapshot(legacy, false, "legacy still needs original bell");
            legacy.OpenShortcuts.Add("canyon.v2.temple-bell-gate");
            await AssertSnapshot(legacy, true, "legacy both original prerequisites");
            TestAssert.True(level.Progress.CollectedFragmentIds.Count == 0,
                "legal exemption does not fabricate collected fragments");

            await AssertSnapshot(Snapshot(6, true, true), true, "six and both original prerequisites");
            Place(level, new Vector2(6120, 1080));
            await Frames(tree, 5);
            ulong bossId = arena.CurrentBoss.GetInstanceId();
            bool realBodyEntered = false;
            var trigger = level.CurrentSectorRoot.GetNode<Area2D>("Gameplay/ActivationTrigger");
            void OnBody(Node2D body) { if (body == level.Player) realBodyEntered = true; }
            trigger.BodyEntered += OnBody;
            KeyEvent(Key.D, true);
            try
            {
                for (int tick = 0; tick < 50 && (!arena.IsEncounterActive || level.Player.GlobalPosition.X - 20 < 6180); tick++)
                    await Frames(tree, 1);
            }
            finally { KeyEvent(Key.D, false); trigger.BodyEntered -= OnBody; }
            await Frames(tree, 3);
            TestAssert.True(realBodyEntered && arena.IsEncounterActive && arena.CurrentBoss.GetInstanceId() == bossId &&
                !entry.Disabled && !exit.Disabled && HullDisabled(true) && Supported(level) &&
                level.Player.GlobalPosition.X - 20 >= 6180,
                "six plus both prerequisites naturally walks across actual trigger: original combat gates close, admission barrier stays open");
            arena.BeginEncounter();
            TestAssert.True(arena.CurrentBoss.GetInstanceId() == bossId && level.Progress.CollectedFragmentIds.Count == 6,
                "repeat activation neither spawns another Boss nor consumes admission fragments");
            arena.ResetForCheckpoint();
            await Frames(tree, 3);
            TestAssert.True(!arena.IsEncounterActive && arena.CurrentBoss.GetInstanceId() != bossId &&
                entry.Disabled && HullDisabled(true) && !exit.Disabled && level.Progress.CollectedFragmentIds.Count == 6,
                "checkpoint reset replaces Boss without losing quota or re-closing eligible admission");
            GD.Print("BOSS_ADMISSION_AND_LEGACY_TRIGGER_RESET_OK controlled_metadata=true native_trigger=true");
            ulong beforeRecoveryBoss = arena.CurrentBoss.GetInstanceId();
            int beforeRecoveries = recoveries;
            level.RequestRespawn("BOSS ADMISSION CONTROLLED RECOVERY");
            for (int tick = 0; tick < 120 && recoveries == beforeRecoveries; tick++) await Frames(tree, 1);
            await Frames(tree, 8);
            TestAssert.True(recoveries == beforeRecoveries + 1 &&
                level.Progress.CollectedFragmentIds.Count == 6 && !level.Progress.LegacyCollectionQuotaExempt &&
                !arena.IsEncounterActive && arena.CurrentBoss.GetInstanceId() != beforeRecoveryBoss &&
                entry.Disabled && HullDisabled(true) && !exit.Disabled &&
                level.Player.GlobalPosition.DistanceTo(new Vector2(5920, 1080)) < 3 && Supported(level),
                "real deferred RequestRespawn/checkpoint pipeline resets Boss, preserves quota and recovers supported at original forecourt");
            GD.Print("BOSS_ADMISSION_LIVE_STATE_AND_ACTUAL_RECOVERY_OK controlled_metadata=true natural_route=false");

            // Restore defeated state before evaluating quota, including completed history.
            foreach (bool completed in new[] { false, true })
            {
                var defeated = Snapshot(0, false, false);
                defeated.BossDefeated = true; defeated.Completed = completed;
                defeated.Objective = "Complete";
                level.RestoreFromSave(defeated.Normalize());
                await Frames(tree, 5);
                GD.Print($"BOSS_ADMISSION_DEFEATED_OBSERVATION completed={completed} progress_defeated={level.Progress.BossDefeated} " +
                    $"boss_null={arena.CurrentBoss == null} active={arena.IsEncounterActive} entry={entry.Disabled} exit={exit.Disabled} " +
                    $"barrier={barrier.Disabled} panel={panel.Visible} count={level.Progress.CollectedFragmentIds.Count}");
                TestAssert.True(arena.CurrentBoss == null && !arena.IsEncounterActive && entry.Disabled &&
                    exit.Disabled && HullDisabled(true) && !panel.Visible && level.Progress.CollectedFragmentIds.Count == 0,
                    "defeated/completed history restores all open gates with no new Boss or manufactured quota");
                arena.BeginEncounter(); arena.ResetForCheckpoint();
                await Frames(tree, 2);
                TestAssert.True(arena.CurrentBoss == null && entry.Disabled && exit.Disabled && HullDisabled(true),
                    "defeated reset and trigger cannot replay encounter or close restored gates");
                AssertHullOcclusion(false);
            }
            GD.Print("BOSS_ADMISSION_DEFEATED_COMPLETED_RESTORE_OK fragments=0 new_boss=false");
        }
        finally
        {
            ReleaseKeys();
            tree.Paused = false;
            if (GodotObject.IsInstanceValid(level))
                await TestSceneTeardown.FreeSceneAfterAudioDrain(tree, level);
        }
    }

    private static void Place(PrologueLevelController level, Vector2 position)
    {
        level.Player.GetNode<MechanicalClawController>("MechanicalClaw").ResetTransientState();
        level.Player.GetNode<PlayerMotor>("Motor").ResetTransientState();
        level.Player.GlobalPosition = position;
        level.Player.Velocity = Vector2.Zero;
    }

    private static Rect2 Rect(CollisionShape2D shape) =>
        new(shape.GlobalPosition - ((RectangleShape2D)shape.Shape).Size / 2, ((RectangleShape2D)shape.Shape).Size);

    private static void AssertRayShape(PrologueLevelController level, Vector2 from, Vector2 to,
        CollisionShape2D expected)
    {
        using var query = PhysicsRayQueryParameters2D.Create(from, to, CombatCollisionLayers.World);
        using var hit = level.Player.GetWorld2D().DirectSpaceState.IntersectRay(query);
        TestAssert.True(hit.Count > 0 && hit["collider"].AsGodotObject() is StaticBody2D body &&
            body == expected.GetParent() &&
            body.ShapeOwnerGetOwner(body.ShapeFindOwner(hit["shape"].AsInt32())) == expected,
            $"native boundary ray {from}->{to} hits the exact authored shape {expected.Name}, not merely its shared body");
    }

    private static void AssertCapsuleOverlap(PrologueLevelController level, Vector2 position,
        CollisionShape2D first, CollisionShape2D second)
    {
        using var query = new PhysicsShapeQueryParameters2D
        {
            Shape = level.Player.GetNode<CollisionShape2D>("BodyCollider").Shape,
            Transform = new Transform2D(0, position), CollisionMask = CombatCollisionLayers.World,
            CollideWithBodies = true, CollideWithAreas = false
        };
        var hits = level.Player.GetWorld2D().DirectSpaceState.IntersectShape(query, 32);
        using var owner = (Godot.Collections.Array)hits;
        bool firstFound = false, secondFound = false;
        foreach (var hit in hits)
        {
            if (hit["collider"].AsGodotObject() is StaticBody2D body)
            {
                GodotObject shapeOwner = body.ShapeOwnerGetOwner(body.ShapeFindOwner(hit["shape"].AsInt32()));
                firstFound |= shapeOwner == first;
                secondFound |= shapeOwner == second;
            }
            hit.Dispose();
        }
        TestAssert.True(firstFound && secondFound,
            $"native actual-sized capsule overlaps both exact corner shapes at {position}; this is a geometric query, not a natural route");
    }

    private static bool ContactShape(CharacterBody2D player, CollisionShape2D expected)
    {
        for (int index = 0; index < player.GetSlideCollisionCount(); index++)
        {
            KinematicCollision2D collision = player.GetSlideCollision(index);
            if (collision.GetCollider() == expected.GetParent() && collision.GetColliderShape() == expected) return true;
        }
        return false;
    }

    private static bool Supported(PrologueLevelController level)
    {
        if (!level.Player.IsOnFloor() || MathF.Abs(level.Player.GlobalPosition.Y + 36 - 1116) > 2f ||
            level.Player.GlobalPosition.X - 20 < 5680 || level.Player.GlobalPosition.X + 20 > 7680) return false;
        // A stationary wall contact can be the sole slide in Godot's latest
        // MoveAndSlide list even while its native floor flag remains true.
        // Independently probe both capsule edges against the actual authored
        // floor; don't accept a flag/nominal Y or a similarly named collider alone.
        foreach (float dx in new[] { -19f, 19f })
        {
            Vector2 point = level.Player.GlobalPosition + new Vector2(dx, 34);
            using var ray = PhysicsRayQueryParameters2D.Create(point, point + Vector2.Down * 4,
                CombatCollisionLayers.World);
            var hit = level.Player.GetWorld2D().DirectSpaceState.IntersectRay(ray);
            if (hit.Count == 0 || hit["collider"].AsGodotObject() is not StaticBody2D body ||
                (body != level.CurrentSectorRoot.GetNode<StaticBody2D>("Gameplay/Collision/ArenaFloor") &&
                 body != level.CurrentSectorRoot.GetNode<StaticBody2D>("Gameplay/Collision/CheckpointFloor")) ||
                hit["normal"].AsVector2().Dot(Vector2.Up) < .7f) return false;
            var shape = body.GetNode<CollisionShape2D>("Collision");
            if (shape.Shape is not RectangleShape2D rectangle ||
                point.X < shape.GlobalPosition.X - rectangle.Size.X / 2 ||
                point.X > shape.GlobalPosition.X + rectangle.Size.X / 2 ||
                MathF.Abs(hit["position"].AsVector2().Y - 1116) > .1f) return false;
        }
        return true;
    }

    private static void KeyEvent(Key key, bool pressed)
    {
        Input.ParseInputEvent(new InputEventKey { PhysicalKeycode = key, Keycode = key, Pressed = pressed });
        Input.FlushBufferedEvents();
    }

    private static void ReleaseKeys()
    {
        foreach (Key key in new[] { Key.A, Key.D, Key.S, Key.W, Key.K, Key.Shift, Key.Space }) KeyEvent(key, false);
    }

    private static PrologueSaveData Snapshot(int count, bool courtyard, bool bell)
    {
        var saved = PrologueSaveData.NewLayoutV2();
        saved.CheckpointId = "canyon.v2.temple-forecourt";
        saved.HasActivatedCheckpoint = true;
        saved.CheckpointSector = "LostTemple";
        saved.CheckpointWorldX = 5920; saved.CheckpointWorldY = 1080;
        for (int index = 0; index < count; index++) saved.CollectedFragmentIds.Add(Fragments[index]);
        if (courtyard) saved.ActivatedMechanisms.Add("canyon.v2.courtyard-seal-open");
        if (bell) saved.OpenShortcuts.Add("canyon.v2.temple-bell-gate");
        return saved.Normalize();
    }

    private static async Task Frames(SceneTree tree, int count)
    {
        for (int frame = 0; frame < count; frame++)
            await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
    }
}
