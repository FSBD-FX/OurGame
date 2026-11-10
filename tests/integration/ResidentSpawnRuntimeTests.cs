using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Knight.Build.Prologue;
using Knight.Combat;
using Knight.Enemies.Residents;
using Knight.Prologue.Residents;
using Knight.Prologue.Seamless;
using Knight.Tools;

namespace Knight.Tests.Integration;

// One actual saved development leaf, actual Level/player/stream. Not fifty
// finished assets, placement certificates, route proof or a new scene builder.
public static class ResidentSpawnRuntimeTests
{
    public static async Task Run(SceneTree tree, string storage)
    {
        ResidentSubtypeDefinition type = SaveLeaf(storage);
        var fixture = new ResidentLevelLifecycleRuntimeTests.Fixture(tree, "resident-atomic-spawn");
        CanyonResidentManager manager = null;
        try
        {
            await fixture.Open(); fixture.Level.Residents.Dispose();
            var slot = Slot("fixture.resident.spawn");
            manager = new(fixture.World, CanyonResidentCatalog.All);
            TestAssert.True(!manager.TrySpawn(CanyonResidentCatalog.All[0].StableId, out _, out _, out var canonical, true) &&
                canonical.Contains("site_or_scene_not_ready") && canonical.Contains("placement_not_authored") && manager.TrackedActorCount == 0,
                "canonical missing placement/unimplemented resources never gain fixture readiness");
            manager.Dispose();
            manager = new(fixture.World, new[] { slot }, new[] { type with { SceneState = ResidentSceneState.Unimplemented, ScenePath = null } });
            TestAssert.True(!manager.TrySpawn(slot.StableId, out _, out _, out var unfinished, true) &&
                unfinished.Contains("scene_not_ready") && !unfinished.Contains("placement_not_authored") && manager.TrackedActorCount == 0,
                "authored placement never grants an unimplemented scene readiness, even with explicit placeholder permission");
            manager.Dispose();
            var other = slot with { StableId = "fixture.resident.overlap" };
            manager = new(fixture.World, new[] { slot, other }, new[] { type });
            TestAssert.True(!manager.TrySpawn(slot.StableId, out _, out _, out var placeholder) &&
                placeholder.Contains("site_or_scene_not_ready") && !manager.Snapshot(slot.StableId).Attached,
                "default runtime admission rejects the same explicit placeholder leaf");
            TestAssert.True(manager.TrySpawn(slot.StableId, out var actor, out var lease, out var issues, true),
                "legitimate actual saved-leaf transaction must commit: " + string.Join("|", issues));
            TestAssert.True(actor.IsNodeReady() && fixture.World.TryGetMountedSector(PrologueSectorId.CanyonDescent, out var sector) &&
                ReferenceEquals(actor.GetParent(), sector.GetNode("Gameplay/Actors")) &&
                actor.GlobalPosition.DistanceTo(new(1510, 1228)) < .001f &&
                manager.Snapshot(slot.StableId) is { Alive: true, Attached: true, Health: 3 } &&
                lease.ActorInstance == actor.GetInstanceId(), "commit uses actual Ready, world pose, direct Actors parent and actual Health");
            TestAssert.True(!manager.TrySpawn(slot.StableId, out _, out _, out _, true) && manager.TrackedActorCount == 1,
                "repeat spawn cannot replace the admitted native identity");
            TestAssert.True(!manager.TrySpawn(other.StableId, out _, out _, out var occupied, true) &&
                occupied.Any(x => x.Contains("resident_attached:")) && manager.TrackedActorCount == 1,
                "same-frame separate slot cannot overlap a committed actor before physics synchronization");
            var health = actor.GetNode<HealthComponent>("Health");
            TestAssert.True(health.ApplyDamage(new(1, Vector2.Right, 0, 53001)) && health.CurrentHealth == 2, "real committed actor loses one HP");
            actor.Free();
            int damageEvents = 0, healEvents = 0, deathEvents = 0;
            void WatchHealth(Node node)
            {
                if (node is not CharacterBody2D body || !body.HasNode("ResidentBinding")) return;
                var value = body.GetNode<HealthComponent>("Health");
                value.Damaged += (_, _) => damageEvents++; value.Healed += (_, _) => healEvents++; value.Died += () => deathEvents++;
            }
            tree.NodeAdded += WatchHealth;
            try
            {
                TestAssert.True(manager.TrySpawn(slot.StableId, out actor, out _, out issues, true) &&
                    actor.GetNode<HealthComponent>("Health").CurrentHealth == 2 && damageEvents + healEvents + deathEvents == 0,
                    "actual transaction restores live HP before Ready without synthetic damage/heal/death: " + string.Join("|", issues));
            }
            finally { tree.NodeAdded -= WatchHealth; }
            TestAssert.True(actor.GetNode<HealthComponent>("Health").ApplyDamage(new(3, Vector2.Right, 0, 53002)), "real restored actor can die");
            actor.Free();
            TestAssert.True(!manager.TrySpawn(slot.StableId, out _, out _, out _, true) && manager.Snapshot(slot.StableId) is { Alive: false, Attached: false },
                "actual death then graph unload does not resurrect a slot");
            manager.BeginRun();
            await ReentryControls(tree, fixture, manager, slot, other, type);
            manager.Dispose();
            await AdmissionControls(fixture, type, slot);
            await InactiveAttackReactivatedDuringMount(tree, fixture, type, slot);
            OriginalSourceMutationOnMount(tree, fixture, type, slot);
            GD.Print("RESIDENT_SPAWN_TRANSACTION_OK saved_leaf=true actual_level=true formal_spawned=0 full_placement=false");
        }
        finally { manager?.Dispose(); await fixture.Close(); }
    }

    private static async Task InactiveAttackReactivatedDuringMount(SceneTree tree,
        ResidentLevelLifecycleRuntimeTests.Fixture fixture, ResidentSubtypeDefinition type, ResidentPopulationDefinition slot)
    {
        using var shape = new RectangleShape2D { Size = new Vector2(26, 18) };
        var attack = new Hitbox { Name = "AtomicAttackReactivationControl",
            CollisionLayer = CombatCollisionLayers.EnemyAttack, CollisionMask = CombatCollisionLayers.Player,
            Monitorable = false };
        attack.AddChild(new CollisionShape2D { Name = "AttackCollider", Shape = shape });
        fixture.World.AddChild(attack);
        attack.GlobalPosition = slot.Placement.WorldSpawn;
        using var manager = new CanyonResidentManager(fixture.World, new[] { slot }, new[] { type });
        CharacterBody2D pending = null;
        bool subscribed = false;
        void Activate(Node node)
        {
            if (node is not CharacterBody2D body || !body.HasNode("ResidentBinding") || pending != null) return;
            pending = body; attack.Begin(78003);
        }
        try
        {
            await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
            tree.NodeAdded += Activate; subscribed = true;
            bool committed;
            IReadOnlyList<string> issues;
            try { committed = manager.TrySpawn(slot.StableId, out _, out _, out issues, true); }
            finally { tree.NodeAdded -= Activate; subscribed = false; }
            TestAssert.True(!committed && pending != null && attack.Monitoring &&
                !GodotObject.IsInstanceValid(pending) && manager.TrackedActorCount == 0 &&
                issues.Any(x => x.Contains("native_hazard")) && manager.Snapshot(slot.StableId) is { Attached: false, Health: null },
                "pre-check inactive attack must not bypass real post-mount reactivation/rollback");
            attack.End();
            TestAssert.True(manager.TrySpawn(slot.StableId, out var fresh, out _, out issues, true),
                "same owned slot may retry after actual End without hidden damage or stale reservation: " + string.Join("|", issues));
            fresh.Free();
            GD.Print("RESIDENT_INACTIVE_ATTACK_POSTMOUNT_OK reactivated_rejected=true owned_rollback=true same_slot_retry=true");
        }
        finally { if (subscribed) tree.NodeAdded -= Activate; if (GodotObject.IsInstanceValid(attack)) attack.Free(); }
    }

    private static Task ReentryControls(SceneTree tree, ResidentLevelLifecycleRuntimeTests.Fixture fixture,
        CanyonResidentManager manager, ResidentPopulationDefinition slot, ResidentPopulationDefinition other, ResidentSubtypeDefinition type)
    {
        CharacterBody2D pending = null; bool hidden = false, sameRejected = false, overlapRejected = false;
        void Reenter(Node node)
        {
            if (node is not CharacterBody2D body || !body.HasNode("ResidentBinding") || pending != null) return;
            pending = body;
            hidden = !body.Visible && body.ProcessMode == Node.ProcessModeEnum.Disabled && !body.IsNodeReady();
            sameRejected = !manager.TrySpawn(slot.StableId, out _, out _, out _, true);
            overlapRejected = !manager.TrySpawn(other.StableId, out _, out _, out var reasons, true) &&
                reasons.Any(x => x.Contains("resident_pending:"));
            manager.BeginRun();
        }
        long before = manager.Epoch;
        tree.NodeAdded += Reenter;
        bool committed;
        try { committed = manager.TrySpawn(slot.StableId, out _, out _, out _, true); }
        finally { tree.NodeAdded -= Reenter; }
        TestAssert.True(!committed && hidden && sameRejected && overlapRejected && pending != null &&
            !GodotObject.IsInstanceValid(pending) && manager.Epoch == before + 1 && manager.TrackedActorCount == 0 &&
            manager.Snapshot(slot.StableId) is { Alive: true, Attached: false, Health: null },
            "NodeAdded reentry reserves identity/body, hides pending actor, rejects old-epoch commit and frees only owned graph");
        TestAssert.True(manager.TrySpawn(slot.StableId, out var fresh, out _, out var issues, true),
            "rollback releases its reservation for a real fresh retry: " + string.Join("|", issues));
        fresh.Free();

        var foreign = new Node2D { Name = "ResidentForeignParent" }; fixture.Level.AddChild(foreign);
        var sentinel = new Node { Name = "NotOwnedByTransaction" }; foreign.AddChild(sentinel);
        pending = null; bool moved = false;
        void MoveOnVisibility(Node node)
        {
            if (node is not CharacterBody2D body || !body.HasNode("ResidentBinding")) return;
            pending = body;
            body.VisibilityChanged += () =>
            {
                if (moved || !body.IsNodeReady() || !body.Visible) return;
                moved = true; body.Reparent(foreign, true);
            };
        }
        tree.NodeAdded += MoveOnVisibility;
        try { committed = manager.TrySpawn(slot.StableId, out _, out _, out _, true); }
        finally { tree.NodeAdded -= MoveOnVisibility; }
        TestAssert.True(!committed && moved && !GodotObject.IsInstanceValid(pending) &&
            GodotObject.IsInstanceValid(sentinel) && ReferenceEquals(sentinel.GetParent(), foreign) && manager.TrackedActorCount == 0,
            "activation reparent rejects commit and cleanup does not touch unrelated foreign children");
        var playerRoot = fixture.Level.Player.GetParent();
        pending = null; moved = false;
        bool playerIdentityPreserved = false;
        void MovePlayerRoot(Node node)
        {
            if (node is not CharacterBody2D body || !body.HasNode("ResidentBinding")) return;
            pending = body;
            body.VisibilityChanged += () =>
            {
                if (moved || !body.IsNodeReady() || !body.Visible) return;
                moved = true; playerRoot.Reparent(foreign, true);
                playerIdentityPreserved = ReferenceEquals(fixture.Level.Player.GetParent(), playerRoot) &&
                    fixture.Level.Player.GetWorld2D() == fixture.World.GetWorld2D() && ReferenceEquals(playerRoot.GetParent(), foreign);
            };
        }
        tree.NodeAdded += MovePlayerRoot;
        CharacterBody2D wronglyAdmitted = null;
        try { committed = manager.TrySpawn(slot.StableId, out wronglyAdmitted, out _, out _, true); }
        finally
        {
            tree.NodeAdded -= MovePlayerRoot;
            playerRoot.Reparent(fixture.Level, true);
            if (GodotObject.IsInstanceValid(wronglyAdmitted)) wronglyAdmitted.Free();
        }
        TestAssert.True(!committed && moved && playerIdentityPreserved && !GodotObject.IsInstanceValid(pending) &&
            manager.TrackedActorCount == 0 && GodotObject.IsInstanceValid(sentinel),
            "moving the whole captured PlayerRoot cannot retain Level ownership merely by preserving player identity and World2D");
        foreign.Free();
        foreach (bool crossSlot in new[] { false, true })
        {
            manager.BeginRun(); bool attempted = false, stolen = false; pending = null;
            void AttemptEarlyRegistration(Node node)
            {
                if (node is not CharacterBody2D body || !body.HasNode("ResidentBinding")) return;
                pending = body;
                body.VisibilityChanged += () =>
                {
                    if (attempted || !body.IsNodeReady() || !body.Visible) return;
                    attempted = true;
                    stolen = manager.TryTrack(crossSlot ? other.StableId : slot.StableId, body, out _);
                };
            }
            tree.NodeAdded += AttemptEarlyRegistration;
            CharacterBody2D result = null;
            try { committed = manager.TrySpawn(slot.StableId, out result, out _, out _, true); }
            finally { tree.NodeAdded -= AttemptEarlyRegistration; }
            bool correctOwner = committed && manager.TrackedActorCount == 1 && manager.Snapshot(slot.StableId).Attached &&
                manager.Snapshot(other.StableId) is { Attached: false, Health: null };
            if (GodotObject.IsInstanceValid(result)) result.Free();
            TestAssert.True(attempted && !stolen && correctOwner,
                "public tracking cannot steal an in-flight actor before atomic commit; cross_slot=" + crossSlot);
        }
        GD.Print("RESIDENT_SPAWN_PENDING_CUSTODY_OK early_same_slot_rejected=true early_cross_slot_rejected=true owner_commit=true");
        GD.Print("RESIDENT_SPAWN_PLAYER_OWNER_OK actual_root_reparent_rejected=true captured_identity_preserved=true");
        pending = null; moved = false;
        void MovePose(Node node)
        {
            if (node is not CharacterBody2D body || !body.HasNode("ResidentBinding")) return;
            pending = body;
            body.VisibilityChanged += () =>
            {
                if (moved || !body.IsNodeReady() || !body.Visible) return;
                moved = true; body.GlobalPosition += new Vector2(40, 0);
            };
        }
        tree.NodeAdded += MovePose;
        try { committed = manager.TrySpawn(slot.StableId, out _, out _, out _, true); }
        finally { tree.NodeAdded -= MovePose; }
        TestAssert.True(!committed && moved && !GodotObject.IsInstanceValid(pending) && manager.TrackedActorCount == 0,
            "activation cannot move a candidate away from the exact native-checked pose");

        CanyonResidentManager replacementOwner = null; CharacterBody2D replacementActor = null;
        pending = null; bool replaced = false, replacementCommitted = false;
        string replacementIssues = "not_attempted";
        // A distinct known body candidate: the cancelling actor is still a
        // solid at its original home until the old transaction unwinds.
        var replacementSlot = slot with { Placement = new(new(1586, 1388), new(1540, 1340, 180, 120),
            new[] { new Vector2(1574, 1388), new Vector2(1598, 1388) }) };
        void ReplaceOwner(Node node)
        {
            if (node is not CharacterBody2D body || !body.HasNode("ResidentBinding") || replaced) return;
            pending = body;
            body.VisibilityChanged += () =>
            {
                if (replaced || !body.IsNodeReady() || !body.Visible) return;
                replaced = true; manager.Dispose();
                replacementOwner = new(fixture.World, new[] { replacementSlot }, new[] { type });
                replacementCommitted = replacementOwner.TrySpawn(slot.StableId, out replacementActor, out _, out var reasons, true);
                replacementIssues = string.Join("|", reasons);
            };
        }
        tree.NodeAdded += ReplaceOwner;
        try
        {
            committed = manager.TrySpawn(slot.StableId, out _, out _, out _, true);
            TestAssert.True(!committed && replaced && replacementCommitted && !GodotObject.IsInstanceValid(pending) &&
                GodotObject.IsInstanceValid(replacementActor) && replacementActor.IsNodeReady() &&
                replacementOwner.TrackedActorCount == 1 && replacementOwner.Snapshot(slot.StableId).Attached,
                "disposed owner rollback cannot delete the fresh owner's same-ID native replacement: " + replacementIssues);
            GD.Print("RESIDENT_SPAWN_OWNER_REPLACEMENT_OK same_id=true old_root_freed=true new_root_preserved=true actual_registry=true");
        }
        finally
        {
            tree.NodeAdded -= ReplaceOwner; replacementOwner?.Dispose();
            if (GodotObject.IsInstanceValid(replacementActor)) replacementActor.Free();
        }
        GD.Print("RESIDENT_SPAWN_REENTRY_OK pending_hidden=true duplicate_rejected=true pending_overlap_rejected=true epoch_rollback=true foreign_child_preserved=true");
        return Task.CompletedTask;
    }

    private static Task AdmissionControls(ResidentLevelLifecycleRuntimeTests.Fixture fixture,
        ResidentSubtypeDefinition type, ResidentPopulationDefinition original)
    {
        // Explicit geometry fixture positions, never a claimed playable route.
        using (var manager = new CanyonResidentManager(fixture.World, new[] { original }, new[] { type with { SceneState = ResidentSceneState.Unimplemented } }))
            TestAssert.True(!manager.TrySpawn(original.StableId, out _, out _, out _, true) && manager.TrackedActorCount == 0,
                "unimplemented subtype rejected even with explicit placeholder opt-in");
        using (var manager = new CanyonResidentManager(fixture.World, new[] { original }, new[] { type }))
        {
            Vector2 before = fixture.Level.Player.GlobalPosition;
            fixture.Level.Player.GlobalPosition = original.Placement.WorldSpawn;
            try
            {
                TestAssert.True(!manager.TrySpawn(original.StableId, out _, out _, out var issues, true) &&
                    issues.Any(x => x.Contains("current_player")) && manager.Snapshot(original.StableId) is { Attached: false, Health: null },
                    "actual captured player plus breathing room blocks birth, not a fake group target");
            }
            finally { fixture.Level.Player.GlobalPosition = before; }
        }
        using (var manager = new CanyonResidentManager(fixture.World, new[] { original }, new[] { type }))
        {
            var viewport = fixture.World.GetViewport(); Transform2D beforeCanvas = viewport.CanvasTransform;
            var worldRoot = (CanvasItem)fixture.World.GetParent();
            bool beforeLevel = fixture.Level.Visible, beforeRoot = worldRoot.Visible, beforeWorld = fixture.World.Visible;
            try
            {
                fixture.Level.Visible = true; worldRoot.Visible = true; fixture.World.Visible = true;
                viewport.CanvasTransform = new(0, viewport.GetVisibleRect().GetCenter() - original.Placement.WorldSpawn);
                TestAssert.True(fixture.World.IsVisibleInTree() &&
                    !manager.TrySpawn(original.StableId, out _, out _, out var issues, true) && issues.Contains("visible_late_birth") &&
                    manager.TrackedActorCount == 0, "actual displayed viewport forbids a visible late birth even far from captured player");
            }
            finally
            {
                viewport.CanvasTransform = beforeCanvas;
                fixture.World.Visible = beforeWorld; worldRoot.Visible = beforeRoot; fixture.Level.Visible = beforeLevel;
            }
        }
        GD.Print("RESIDENT_SPAWN_VISIBLE_BIRTH_OK actual_canvas=true rejected=true player_distant=true");
        var old = original with { Placement = new(new(1550, 1604), new(1480, 1540, 160, 150),
            new[] { new Vector2(1538, 1604), new Vector2(1562, 1604) }) };
        using (var manager = new CanyonResidentManager(fixture.World, new[] { old }, new[] { type }))
            TestAssert.True(!manager.TrySpawn(old.StableId, out _, out _, out var issues, true) && issues.Any(x => x.Contains("old-spawn:")),
                "future original wave spawn is reserved even before its actor appears");
        var unsupported = original with { Placement = new(new(1730, 1300), new(1660, 1230, 160, 150),
            new[] { new Vector2(1718, 1300), new Vector2(1742, 1300) }) };
        using (var manager = new CanyonResidentManager(fixture.World, new[] { unsupported }, new[] { type }))
            TestAssert.True(!manager.TrySpawn(unsupported.StableId, out _, out _, out var issues, true) && issues.Any(x => x.Contains("support")),
                "native support failure is explicit and does not silently skip/attach the slot");
        GD.Print("RESIDENT_SPAWN_ADMISSION_OK default_placeholder_rejected=true canonical_unready_rejected=true actual_player_protected=true original_future_reserved=true native_support_required=true");
        return Task.CompletedTask;
    }

    private static void OriginalSourceMutationOnMount(SceneTree tree, ResidentLevelLifecycleRuntimeTests.Fixture fixture,
        ResidentSubtypeDefinition type, ResidentPopulationDefinition slot)
    {
        PackedScene source = GD.Load<PackedScene>(CanyonEncounterCatalog.MothScene);
        SceneState state = source.GetState();
        CircleShape2D shape = null;
        for (int node = 0; node < state.GetNodeCount(); node++)
        {
            using NodePath path = state.GetNodePath(node);
            string relative = path.ToString();
            if (relative.StartsWith("./", StringComparison.Ordinal)) relative = relative[2..];
            if (relative != "BodyCollider") continue;
            for (int property = 0; property < state.GetNodePropertyCount(node); property++)
            {
                using StringName name = state.GetNodePropertyName(node, property);
                if (name.ToString() != "shape") continue;
                using Variant value = state.GetNodePropertyValue(node, property);
                shape = value.AsGodotObject() as CircleShape2D;
            }
        }
        TestAssert.True(shape != null && shape.Radius == 16,
            $"negative control reaches the actual old Moth saved body, not the resident capsule; found={shape != null} radius={shape?.Radius}");
        float radius = shape.Radius;
        bool changed = false;
        CharacterBody2D pending = null;
        using var manager = new CanyonResidentManager(fixture.World, new[] { slot }, new[] { type });
        void Mutate(Node node)
        {
            if (changed || node is not CharacterBody2D body || !body.HasNode("ResidentBinding")) return;
            pending = body; changed = true;
            shape.Radius = 5000;
        }
        tree.NodeAdded += Mutate;
        try
        {
            TestAssert.True(!manager.TrySpawn(slot.StableId, out _, out _, out var issues, true) && changed &&
                pending != null && !GodotObject.IsInstanceValid(pending) &&
                issues.Any(x => x.Contains("old-spawn:")) && manager.TrackedActorCount == 0 &&
                manager.Snapshot(slot.StableId) is { Attached: false, Health: null },
                "post-mount admission must reread changed old saved geometry and reject/release only the pending actor");
        }
        finally { tree.NodeAdded -= Mutate; shape.Radius = radius; }
        TestAssert.True(manager.TrySpawn(slot.StableId, out var restored, out _, out var restoredIssues, true),
            "same manager rechecks restored saved geometry rather than caching the rejection: " + string.Join('|', restoredIssues));
        restored.Free();
        GD.Print("RESIDENT_ORIGINAL_SOURCE_POSTMOUNT_OK actual_shared_shape_mutation=true second_admission_rejected=true pending_released=true restored_shape_reaccepted=true");
    }

    private static ResidentPopulationDefinition Slot(string id) => new(id, "rock-wanderer", CanyonRouteNodeId.ArchiveSideCave,
        PrologueSectorId.CanyonDescent, ResidentFamily.Native, ResidentArchetype.Wanderer,
        new(new(1510, 1228), new(1480, 1176, 120, 100), new[] { new Vector2(1498, 1228), new Vector2(1522, 1228) }));

    private static ResidentSubtypeDefinition SaveLeaf(string storage)
    {
        var source = CanyonResidentCatalog.Subtypes.Single(x => x.Id == "rock-wanderer");
        string path = Path.Combine(storage, "spawn-rock-wanderer.tscn");
        TestAssert.True(!File.Exists(path), "one-leaf fixture uses fresh case output");
        using var profile = new ResidentSubtypeProfile { SubtypeId = source.Id, Family = source.Family,
            Archetype = source.Archetype, Carrier = source.Carrier };
        var author = GD.Load<PackedScene>(source.ProbeScenePath).Instantiate<CharacterBody2D>();
        try
        {
            author.SceneFilePath = "";
            author.AddChild(new ResidentSceneBinding { Name = "ResidentBinding", Profile = profile });
            TestAssert.Equal(Error.Ok, SceneBuildUtil.PackAndSave(author, path), "save real actor graph and exported binding");
        }
        finally { author.Free(); }
        return source with { ScenePath = path, SceneState = ResidentSceneState.ExplicitPlaceholder };
    }
}
