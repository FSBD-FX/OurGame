using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Knight.Build.Prologue;
using Knight.Enemies.Residents;
using Knight.Prologue.Seamless;

namespace Knight.Prologue.Residents;

public sealed partial class CanyonResidentManager
{
    [ThreadStatic] private static ResidentFirstInstantiateTrace _residentFirstInstantiateTrace;

    // Scoped to one admitted headless registry case. Holds scalars only and
    // writes to the same OS stderr stream as the native diagnostics.
    internal sealed class ResidentFirstInstantiateTrace : IDisposable
    {
        private const int MaxOperations = 256;
        private readonly string _arm;
        private int _operations;
        private bool _overflow, _writeFailed;
        internal bool Complete => _operations > 0 && !_overflow && !_writeFailed;

        internal ResidentFirstInstantiateTrace(string arm)
        {
            if (_residentFirstInstantiateTrace != null)
                throw new InvalidOperationException("Resident first instantiate trace is already active");
            _arm = arm;
            _residentFirstInstantiateTrace = this;
        }

        internal int Enter(string owner, long epoch, long reset, string slot, string scene)
        {
            if (_operations < MaxOperations)
            {
                int operation = ++_operations;
                WriteBoundary(operation, "enter", owner, epoch, reset, slot, scene);
                return operation;
            }
            if (!_overflow)
            {
                _overflow = true;
                Write($"RESIDENT_FIRST_INSTANTIATE_LIMIT arm={_arm} max_ops={MaxOperations}");
            }
            return 0;
        }

        internal void WriteBoundary(int operation, string stage, string owner, long epoch, long reset,
            string slot, string scene, string exception = null)
        {
            if (operation == 0) return;
            Write($"RESIDENT_FIRST_INSTANTIATE arm={_arm} op={operation} stage={stage} owner={owner} " +
                $"epoch={epoch} reset={reset} slot={slot} scene={scene}" +
                (exception == null ? "" : $" exception={exception}"));
        }

        private void Write(string row)
        {
            try { Console.Error.WriteLine(row); }
            catch (Exception) { _writeFailed = true; } // Observation must not replace the original exception.
        }

        public void Dispose()
        {
            if (ReferenceEquals(_residentFirstInstantiateTrace, this)) _residentFirstInstantiateTrace = null;
        }
    }

    private sealed class SpawnClaim { public Rect2? Bounds; public CharacterBody2D Actor; }
    private readonly Dictionary<string, SpawnClaim> _spawning = new(StringComparer.Ordinal);
    // These nine immutable catalog paths belong to this world's manager, not
    // a static cache. Keep source wrappers while needed; still reread geometry
    // at BOTH admission boundaries so native shape edits remain observable.
    private readonly ResidentOriginalSceneSources _originalSources = new(
        CanyonEncounterCatalog.Normal.SelectMany(x => x.Enemies).Select(x => x.ScenePath),
        path => GD.Load<PackedScene>(path));

    // This transaction checks current geometry and custody, NOT complete attack,
    // motion, player-route or final-art readiness.
    public bool TrySpawn(string stableId, out CharacterBody2D actor, out ResidentLease lease,
        out IReadOnlyList<string> issues, bool allowPlaceholder = false)
        => TrySpawnCore(stableId, out actor, out lease, out issues, allowPlaceholder, null);

    internal bool TrySpawnAuthorized(string stableId, out CharacterBody2D actor, out ResidentLease lease,
        out IReadOnlyList<string> issues, bool allowPlaceholder, Func<bool> admissionAuthorized)
        => TrySpawnCore(stableId, out actor, out lease, out issues, allowPlaceholder, admissionAuthorized);

    private bool TrySpawnCore(string stableId, out CharacterBody2D actor, out ResidentLease lease,
        out IReadOnlyList<string> issues, bool allowPlaceholder, Func<bool> admissionAuthorized)
    {
        actor = null; lease = default;
#if REFCOUNTED_FATAL_IDENTITY
        bool tracePreparationEntered = false;
#endif
        var errors = new List<string>(); issues = errors.AsReadOnly();
        if (admissionAuthorized != null && !admissionAuthorized()) return Reject("caller_cancelled");
        if (_disposed || stableId == null || !_definitions.TryGetValue(stableId, out var slot)) return Reject("unknown_or_disposed");
        var before = _state.Snapshot(stableId);
        if (!before.Alive || before.Attached || _spawning.ContainsKey(stableId)) return Reject("slot_unavailable");
        if (slot.Placement == null)
        { errors.Add("site_or_scene_not_ready"); return Reject("placement_not_authored"); }
        if (!_subtypes.TryGetValue(slot.SubtypeId, out var type) ||
            type.SceneState == ResidentSceneState.Unimplemented || string.IsNullOrWhiteSpace(type.ScenePath) ||
            !allowPlaceholder && type.SceneState != ResidentSceneState.Implemented)
        { errors.Add("site_or_scene_not_ready"); return Reject("scene_not_ready"); }
        if (!_world.TryGetResidentSpawnContext(slot.Sector, out var sector, out var parent, out var player, out var bounds))
            return Reject("invalid_spawn_context");
        var region = CanyonWorldLayout.Nodes.SingleOrDefault(x => x.Id == slot.RouteNode);
        if (region == null || region.SectorId != slot.Sector || !Positive(slot.Placement.Territory) ||
            !bounds.Encloses(slot.Placement.Territory) || !region.WorldBounds.Encloses(slot.Placement.Territory))
            return Reject("territory_outside_authored_region");
        var claim = new SpawnClaim(); _spawning.Add(stableId, claim);
        CharacterBody2D owned = null; bool committed = false;
        Transform2D intended = new(0, slot.Placement.WorldSpawn);
#if REFCOUNTED_FATAL_IDENTITY
        using var profilePreparation = Knight.Tests.Diagnostics.ResidentProfileLifetimeTrace.BeginPreparation(
            _ownerId, before.Epoch, before.ResetRevision, stableId, type.ScenePath);
        ulong tracedActorId = 0;
        tracePreparationEntered = true;
#endif
        try
        {
            // The scene path and immutable binding come from this manager's
            // catalog, not a caller-supplied substitute definition or factory.
            var saved = _residentSources.Get(type.ScenePath);
            var firstTrace = _residentFirstInstantiateTrace;
            int firstOperation = firstTrace?.Enter(_ownerId, before.Epoch, before.ResetRevision, stableId, type.ScenePath) ?? 0;
            try { owned = ResidentSceneBinding.Instantiate(saved, type, slot, allowPlaceholder); }
            catch (Exception error) when (firstOperation != 0)
            {
                firstTrace.WriteBoundary(firstOperation, "fault", _ownerId, before.Epoch, before.ResetRevision,
                    stableId, type.ScenePath, error.GetType().FullName);
                throw;
            }
            firstTrace?.WriteBoundary(firstOperation, "return", _ownerId, before.Epoch, before.ResetRevision, stableId, type.ScenePath);
#if REFCOUNTED_FATAL_IDENTITY
            tracedActorId = Knight.Tests.Diagnostics.ResidentProfileLifetimeTrace.PreparedActorId();
            Knight.Tests.Diagnostics.ResidentProfileLifetimeTrace.Stage("configured-return", tracedActorId);
#endif
            claim.Actor = owned;
            if (owned.TopLevel) return Reject("top_level_actor");
            if (before.Health.HasValue && !TryPrepareRestoredHealth(stableId, owned)) return Reject("health_preparation_rejected");
            owned.Transform = parent.GlobalTransform.AffineInverse() * intended;
            if (!StillAuthorized()) return Reject("spawn_context_changed");
            if (!CheckAdmission(owned, intended, slot, player, claim, errors)) return false;
            bool visible = owned.Visible; var processMode = owned.ProcessMode;
            owned.Visible = false; owned.ProcessMode = Node.ProcessModeEnum.Disabled;
            ((IResidentEnemyAgent)owned).SetResidentDormant(true);
            parent.AddChild(owned); // Ready/NodeAdded are reentrant boundaries.
#if REFCOUNTED_FATAL_IDENTITY
            Knight.Tests.Diagnostics.ResidentProfileLifetimeTrace.Stage("mount-return", tracedActorId);
#endif
            if (!StillAuthorized() || !Live(owned) || !ReferenceEquals(owned.GetParent(), parent) ||
                !Near(owned.GlobalTransform, intended)) return Reject("mount_context_changed");
#if REFCOUNTED_FATAL_IDENTITY
            Knight.Tests.Diagnostics.ResidentProfileLifetimeTrace.Stage("mount-gate-passed", tracedActorId);
#endif
            // Activation can emit signals too. Commit the scalar lease LAST,
            // after activation and one final native/body/context recheck.
            ((IResidentEnemyAgent)owned).SetResidentDormant(false);
            owned.ProcessMode = processMode; owned.Visible = visible;
#if REFCOUNTED_FATAL_IDENTITY
            Knight.Tests.Diagnostics.ResidentProfileLifetimeTrace.Stage("activation-return", tracedActorId);
#endif
            if (!StillAuthorized() || !Live(owned) || !ReferenceEquals(owned.GetParent(), parent) ||
                !Near(owned.GlobalTransform, intended)) return Reject("activation_context_changed");
#if REFCOUNTED_FATAL_IDENTITY
            Knight.Tests.Diagnostics.ResidentProfileLifetimeTrace.Stage("activation-gate-passed", tracedActorId);
#endif
            if (!CheckAdmission(owned, intended, slot, player, claim, errors)) return false;
            if (!StillAuthorized() || !Live(owned) || !ReferenceEquals(owned.GetParent(), parent) ||
                !Near(owned.GlobalTransform, intended) || !TryTrackCore(stableId, owned, out var admitted, claim)) return Reject("commit_rejected");
            actor = owned; lease = admitted; committed = true; return true;
        }
        catch (Exception error) { return Reject("spawn_exception:" + error.GetType().Name + ":" + error.Message); }
        finally
        {
            if (!committed && GodotObject.IsInstanceValid(owned))
            {
                // Never revoke a replacement by stable ID alone, even after a
                // reentrant reset/new run. Only this call's exact native root.
                if (_actors.TryGetValue(stableId, out var binding) && ReferenceEquals(binding.Actor, owned))
                    Release(binding.Lease, captureHealth: false);
                owned.Free();
            }
            if (_spawning.TryGetValue(stableId, out var current) && ReferenceEquals(current, claim)) _spawning.Remove(stableId);
        }

        bool Reject(string reason)
        {
#if REFCOUNTED_FATAL_IDENTITY
            if (tracePreparationEntered)
                Knight.Tests.Diagnostics.ResidentProfileLifetimeTrace.Stage("spawn-rejected", detail: reason);
#endif
            errors.Add(reason); return false;
        }
        bool StillAuthorized() => (admissionAuthorized == null || admissionAuthorized()) &&
            !_disposed && _state.Epoch == before.Epoch &&
            _state.Snapshot(stableId) is { Alive: true, Attached: false } current && current.ResetRevision == before.ResetRevision &&
            _spawning.TryGetValue(stableId, out var pending) && ReferenceEquals(pending, claim) &&
            _world.TryGetResidentSpawnContext(slot.Sector, out var actual, out var actors, out var actualPlayer, out var actualBounds) &&
            ReferenceEquals(actual, sector) && ReferenceEquals(actors, parent) && ReferenceEquals(actualPlayer, player) && actualBounds == bounds;
    }

    private bool CheckAdmission(CharacterBody2D actor, Transform2D intended, ResidentPopulationDefinition slot,
        CharacterBody2D player, SpawnClaim claim, List<string> errors)
    {
        var environment = ResidentEnvironmentInventory.Capture(_world);
        errors.AddRange(environment.Issues);
        var keepOut = new List<ResidentKeepOut>(environment.Regions);
        CaptureOriginalSpawns(keepOut, errors, _originalSources.Get);
        if (!TryBodyBounds(player, player.GlobalTransform, out var playerBounds) ||
            !PrologueSectorDefinition.Finite(player.Velocity)) errors.Add("invalid_player_body");
        else
        {
            // NEW admission policy: conservative quarter-second motion envelope
            // plus 64 px breathing room. Not a traversal/landing certificate.
            Rect2 predicted = new(playerBounds.Position + player.Velocity * .25f, playerBounds.Size);
            keepOut.Add(new("current_player", ResidentKeepOutKind.Protected, playerBounds.Merge(predicted).Grow(64)));
        }
        foreach (var binding in _actors.Values.Where(Authorized))
            if (TryBodyBounds(binding.Actor, binding.Actor.GlobalTransform, out var occupied))
                keepOut.Add(new("resident_attached:" + binding.Lease.StableId, ResidentKeepOutKind.OriginalEnemy, occupied));
            else errors.Add("invalid_attached_body:" + binding.Lease.StableId);
        foreach (var entry in _spawning.Where(x => !ReferenceEquals(x.Value, claim)))
            if (entry.Value.Bounds is Rect2 occupied) keepOut.Add(new("resident_pending:" + entry.Key, ResidentKeepOutKind.OriginalEnemy, occupied));
        var playerBody = player.GetNodeOrNull<CollisionShape2D>("BodyCollider");
        var playerHurtbox = player.GetNodeOrNull<Area2D>("Hurtbox");
        var playerHurtShape = playerHurtbox?.GetNodeOrNull<CollisionShape2D>("HurtboxCollider");
        if (!GodotObject.IsInstanceValid(playerBody) || !GodotObject.IsInstanceValid(playerHurtbox) ||
            !GodotObject.IsInstanceValid(playerHurtShape)) errors.Add("invalid_player_collision_identity");
        if (errors.Count != 0) return false;
        var playerIdentity = new ResidentPlayerCollisionIdentity(player.GetInstanceId(), playerBody.GetInstanceId(),
            playerHurtbox.GetInstanceId(), playerHurtShape.GetInstanceId());
        bool ground = slot.Archetype is not (ResidentArchetype.Flyer or ResidentArchetype.Diver);
        var space = _world.GetWorld2D().DirectSpaceState;
        // This table exists only inside THIS synchronous admission. Ready and
        // activation still separate the two fresh CheckAdmission calls above.
        var patrol = ResidentPlacementGeometry.CheckPatrol(actor, intended,
            slot.Placement.WorldSpawn, slot.Placement.PatrolPoints, space, _world,
            slot.Placement.Territory, ground, keepOut, playerIdentity);
        var body = patrol.Body;
        errors.AddRange(body.Issues);
        foreach (ResidentPathGeometryResult corridor in patrol.Corridors)
            errors.AddRange(corridor.Issues);
        // Check actual rendering transform; hidden title/world may initialize,
        // but a late birth inside the displayed gameplay viewport is rejected.
        if (_world.IsVisibleInTree())
        {
            var viewport = _world.GetViewport();
            Rect2 screen = viewport.CanvasTransform * body.WorldBodyBounds;
            if (!Positive(screen) || viewport.GetVisibleRect().Intersects(screen)) errors.Add("visible_late_birth");
        }
        if (errors.Count != 0) return false;
        claim.Bounds = body.WorldBodyBounds; return true;
    }

    private static void CaptureOriginalSpawns(List<ResidentKeepOut> regions, List<string> errors, Func<string, PackedScene> source)
    {
        // Bounds are per-check only. Retained sources are owner-scoped; never
        // retain shape/state wrappers or skip the fresh post-activation reader.
        var bodies = new Dictionary<string, Rect2>(StringComparer.Ordinal);
        foreach (var encounter in CanyonEncounterCatalog.Normal)
        for (int index = 0; index < encounter.Enemies.Count; index++)
        {
            var enemy = encounter.Enemies[index];
            if (!bodies.TryGetValue(enemy.ScenePath, out var local))
            {
#if REFCOUNTED_FATAL_IDENTITY
                string traceSlot = encounter.StableId + ":" + index;
                long traceOperation = Knight.Tests.Diagnostics.Task14NativeResourceIdentity.BeginOriginalProbe(traceSlot, enemy.ScenePath, "source-acquire-enter");
                try
                {
                    PackedScene saved = source(enemy.ScenePath);
                    Knight.Tests.Diagnostics.Task14NativeResourceIdentity.OriginalProbeBoundary(traceOperation, "source-acquire-return", traceSlot, enemy.ScenePath, 0);
                    Knight.Tests.Diagnostics.Task14NativeResourceIdentity.OriginalProbeBoundary(traceOperation, "id-read-enter", traceSlot, enemy.ScenePath, 0);
                    ulong savedId = saved == null ? 0 : saved.GetInstanceId();
                    Knight.Tests.Diagnostics.Task14NativeResourceIdentity.OriginalProbeBoundary(traceOperation, "id-read-return", traceSlot, enemy.ScenePath, savedId);
                    Knight.Tests.Diagnostics.Task14NativeResourceIdentity.OriginalProbeBoundary(traceOperation, "saved-reader-enter", traceSlot, enemy.ScenePath, savedId);
                    bool read = ResidentSavedBodyGeometry.TryRead(saved, out local, out string reason);
                    Knight.Tests.Diagnostics.Task14NativeResourceIdentity.OriginalProbeBoundary(traceOperation, "saved-reader-return:" + read, traceSlot, enemy.ScenePath, savedId);
                    if (!read)
                    {
                        errors.Add("invalid_original_body:" + enemy.ScenePath + ":" + reason);
                        continue;
                    }
                }
                finally { Knight.Tests.Diagnostics.Task14NativeResourceIdentity.EndOriginalProbe(traceOperation); }
#else
                if (!ResidentSavedBodyGeometry.TryRead(source(enemy.ScenePath), out local, out string reason))
                {
                    errors.Add("invalid_original_body:" + enemy.ScenePath + ":" + reason);
                    continue;
                }
#endif
                bodies.Add(enemy.ScenePath, local);
            }
            var points = new List<(string Tag, Vector2 Point)> { ("primary", enemy.WorldPosition) };
            if (enemy.AlternateWorldPosition is Vector2 alternate) points.Add(("alternate", alternate));
            foreach (var point in points)
            {
                Rect2 bounds = new(local.Position + point.Point, local.Size);
                if (encounter.RouteNode == CanyonRouteNodeId.CounterweightLift && enemy.ScenePath == CanyonEncounterCatalog.RustShellScene)
                    bounds = bounds.Merge(new Rect2(bounds.Position + new Vector2(0, 4), bounds.Size));
                regions.Add(new("old-spawn:" + encounter.StableId + ":" + index + ":" + point.Tag, ResidentKeepOutKind.OriginalEnemy, bounds));
            }
        }
    }

    private static bool TryBodyBounds(CharacterBody2D actor, Transform2D pose, out Rect2 bounds)
    {
        bounds = default;
        if (!GodotObject.IsInstanceValid(actor)) return false;
        var physical = Walk(actor).OfType<CollisionShape2D>().Where(x => NearestBody(x) == actor).ToArray();
        if (physical.Length != 1 || physical[0].Name != "BodyCollider" || physical[0].Disabled || physical[0].OneWayCollision ||
            physical[0].Shape is not (CapsuleShape2D or RectangleShape2D or CircleShape2D) ||
            Walk(actor).OfType<CollisionPolygon2D>().Any(x => NearestBody(x) == actor)) return false;
        Transform2D relative = Transform2D.Identity;
        for (Node node = physical[0]; node != actor; node = node.GetParent())
        {
            if (node is not Node2D local || local.TopLevel) return false;
            relative = local.Transform * relative;
        }
        bounds = pose * relative * physical[0].Shape.GetRect(); return Positive(bounds);
    }
    private static CollisionObject2D NearestBody(Node node)
    {
        for (Node parent = node.GetParent(); parent != null; parent = parent.GetParent())
            if (parent is CollisionObject2D body) return body;
        return null;
    }
    private static IEnumerable<Node> Walk(Node root)
    {
        yield return root;
        for (int i = 0; i < root.GetChildCount(); i++) foreach (Node child in Walk(root.GetChild(i))) yield return child;
    }
    private static bool Positive(Rect2 bounds) => PrologueSectorDefinition.FinitePositive(bounds);
    private static bool Near(Transform2D left, Transform2D right) => left.X.DistanceTo(right.X) < .001f &&
        left.Y.DistanceTo(right.Y) < .001f && left.Origin.DistanceTo(right.Origin) < .001f;
}
