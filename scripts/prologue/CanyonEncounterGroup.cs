using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Knight.Build.Prologue;
using Knight.Combat;
using Knight.Enemies.Ranged;
using Knight.Presentation;
using Knight.Prologue.Seamless;

namespace Knight.Prologue;

public partial class CanyonEncounterGroup : Node, ICheckpointResettable
{
    [Signal] public delegate void CompletedEventHandler(StringName encounterId);
    [Signal] public delegate void GroupDefeatedEventHandler(StringName stableId);
    [Signal] public delegate void ArrivalSuppressedEventHandler(StringName stableId);

    [Export] public StringName EncounterId { get; set; } = "canyon.v2.encounter.split-level";
    [Export] public NodePath ActorRootPath { get; set; } = new("../../Actors");

    public CanyonEncounterDefinition Definition { get; set; }
    public bool IsComplete { get; private set; }
    public bool IsActive { get; private set; }
    public int CurrentBatch { get; private set; } = -1;
    public int LiveActorCount => _actors.Count;
    public bool IsArrivalWarningActive { get; private set; }
    public bool SpawnSuppressed { get; private set; }

    private readonly List<ActorBinding> _actors = new();
    private PrologueProgress _progress;
    private bool _retiring;
    private ulong _lifecycleGeneration;
    private bool _batchSpawnPending;
    private double _arrivalRemaining;
    private double _suppressedRetryRemaining;
    private readonly List<CanyonSpawnWarningVisual> _arrivalWarnings = new();
    private const double ArrivalSeconds = 0.60d;
    private const double SuppressedRetrySeconds = 0.25d;

    public override void _Ready()
    {
        ResolveDefinition();
        SetPhysicsProcess(true);
    }

    public override void _PhysicsProcess(double delta)
    {
        if (IsComplete || !GodotObject.IsInstanceValid(this) || !IsInsideTree() || IsQueuedForDeletion()) return;
        CanyonEncounterDefinition definition = ResolveDefinition();
        if (definition == null) return;
        Node2D player = GetTree()?.GetFirstNodeInGroup("player") as Node2D;
        if (!GodotObject.IsInstanceValid(player) || player.IsQueuedForDeletion() ||
            player.GetNodeOrNull<HealthComponent>("Health") is { CurrentHealth: <= 0 })
        {
            if (IsActive) RetireLiveActors();
            return;
        }

        if (IsActive)
        {
            if (!definition.ArenaBounds.HasPoint(player.GlobalPosition) &&
                !InsideGearGalleryContinuation(definition, player.GlobalPosition) &&
                !SupportedFrozenLeftOverhang(definition, player))
                RetireLiveActors();
            else if (IsArrivalWarningActive)
            {
                _arrivalRemaining -= delta;
                if (_arrivalRemaining <= 0d) FinishArrival();
            }
            else if (SpawnSuppressed && !_batchSpawnPending && _actors.Count == 0)
                RetrySuppressedArrival(delta);
            return;
        }

        if (definition.ActivationBounds.HasPoint(player.GlobalPosition) &&
            ActivationLessonSatisfied(definition, player))
            BeginEncounter();
    }

    public void ApplyProgress(PrologueProgress progress)
    {
        _progress = progress;
        CanyonEncounterDefinition definition = ResolveDefinition();
        IsComplete = definition?.GatesProgression == true &&
            progress?.ActivatedMechanisms.Contains(EncounterId.ToString()) == true;
        if (IsComplete) RetireLiveActors(resetBatch: false);
    }

    // Retained for Task5 prerequisite fixtures. Production completion is driven only by
    // subscribed IEncounterActor.Defeated callbacks in OnActorDefeated.
    public bool Complete()
    {
        if (IsComplete) return false;
        RetireLiveActors(resetBatch: false);
        IsComplete = true;
        _progress?.ActivateMechanism(EncounterId.ToString());
        EmitSignal(SignalName.Completed, EncounterId);
        return true;
    }

    public void ResetForCheckpoint()
    {
        RetireLiveActors();
        CanyonEncounterDefinition definition = ResolveDefinition();
        IsComplete = definition?.GatesProgression == true &&
            _progress?.ActivatedMechanisms.Contains(EncounterId.ToString()) == true;
    }

    public override void _ExitTree() => RetireLiveActors();

    private void BeginEncounter()
    {
        SpawnSuppressed = false;
        _suppressedRetryRemaining = 0d;
        IsActive = true;
        CurrentBatch = 0;
        SpawnBatch(CurrentBatch);
    }

    private void SpawnBatch(int batch)
    {
        CanyonEncounterDefinition definition = ResolveDefinition() ??
            throw new InvalidOperationException($"Encounter '{EncounterId}' has no authored definition.");
        Node actorRoot = GetNodeOrNull(ActorRootPath) ??
            throw new InvalidOperationException(
                $"Encounter '{EncounterId}' ActorRootPath '{ActorRootPath}' does not resolve.");
        foreach (CanyonEnemyPlacement placement in definition.Enemies.Where(enemy =>
                     enemy.ActivationBatch == batch))
            SpawnPlacement(actorRoot, placement, batch, placement.WorldPosition);
        if (_actors.Count == 0)
            throw new InvalidOperationException(
                $"Encounter '{EncounterId}' activation batch {batch} has no authored enemies.");
    }

    private void SpawnPlacement(Node actorRoot, CanyonEnemyPlacement placement, int batch,
        Vector2 position)
    {
        PackedScene scene = GD.Load<PackedScene>(placement.ScenePath) ??
            throw new InvalidOperationException(
                $"Encounter '{EncounterId}' cannot load '{placement.ScenePath}'.");
#if RANGED_SPAWN_IDENTITY
        var spawnIdentity = Knight.Tests.Integration.RangedSpawnIdentityTrace.Begin(this, "live",
            placement.ScenePath, scene, batch, _lifecycleGeneration);
#endif
        Node instance = scene.Instantiate();
#if RANGED_SPAWN_IDENTITY
        Knight.Tests.Integration.RangedSpawnIdentityTrace.Returned(spawnIdentity, instance);
#endif
        if (instance is not Node2D actorNode || instance is not IEncounterActor actor)
        {
            instance.Free();
            throw new InvalidOperationException(
                $"Encounter '{EncounterId}' scene '{placement.ScenePath}' must be Node2D + IEncounterActor.");
        }
        actorNode.Name = $"{EncounterId.ToString().Replace('.', '_')}_batch{batch}_{_actors.Count + 1}";
        PositionBeforeMount(actorNode, actorRoot, position);
        if (actorNode is RangedWeaponEnemy ranged)
            ranged.ConfigureEncounterEnvelope(ResolveDefinition().ArenaBounds);
        actorRoot.AddChild(actorNode);
        Action defeated = null;
        defeated = () => OnActorDefeated(actorNode, actor, defeated);
        actor.Defeated += defeated;
        _actors.Add(new ActorBinding(actorNode, actor, defeated));
    }

    private void OnActorDefeated(Node2D actorNode, IEncounterActor actor, Action defeated)
    {
        if (_retiring || !IsActive || IsComplete) return;
        int index = _actors.FindIndex(binding => binding.Node == actorNode &&
                                                 ReferenceEquals(binding.Actor, actor));
        if (index < 0) return;
        actor.Defeated -= defeated;
        _actors.RemoveAt(index);
        if (GodotObject.IsInstanceValid(actorNode)) actorNode.QueueFree();
        if (_actors.Count > 0) return;

        CanyonEncounterDefinition definition = ResolveDefinition();
        int finalBatch = definition.Enemies.Max(enemy => enemy.ActivationBatch);
        if (CurrentBatch < finalBatch)
        {
            CurrentBatch++;
            int batch = CurrentBatch;
            ulong generation = _lifecycleGeneration;
            _batchSpawnPending = true;
            CanyonEnemyPlacement[] next = definition.Enemies.Where(enemy =>
                enemy.ActivationBatch == batch).ToArray();
            if (next.Length > 1 || next.Any(enemy => enemy.AlternateWorldPosition.HasValue))
                Callable.From(() => BeginArrival(batch, generation)).CallDeferred();
            else
                Callable.From(() => SpawnDeferredBatch(batch, generation)).CallDeferred();
            return;
        }
        CompleteFromDefeats(definition);
    }

    private void SpawnDeferredBatch(int batch, ulong generation)
    {
        if (!_batchSpawnPending || generation != _lifecycleGeneration || !IsActive ||
            IsComplete || CurrentBatch != batch || _actors.Count != 0)
            return;
        if (CurrentAdmittedPlayer() == null) { RetireLiveActors(); return; }
        _batchSpawnPending = false;
        SpawnBatch(batch);
    }

    private void BeginArrival(int batch, ulong generation)
    {
        if (!_batchSpawnPending || !OwnsArrival(batch, generation) || IsArrivalWarningActive)
            return;
        if (CurrentAdmittedPlayer() == null) { RetireLiveActors(); return; }
        Node actorRoot = GetNodeOrNull(ActorRootPath);
        if (!GodotObject.IsInstanceValid(actorRoot) || !actorRoot.IsInsideTree() || actorRoot.IsQueuedForDeletion())
        {
            RetireLiveActors();
            return;
        }
        _batchSpawnPending = false;
        SpawnSuppressed = false;
        _suppressedRetryRemaining = 0d;
        foreach (Vector2 site in ResolveDefinition().Enemies.Where(enemy => enemy.ActivationBatch == batch)
                     .SelectMany(CandidateSites).Distinct())
        {
            var warning = new CanyonSpawnWarningVisual { Name = "ArrivalWarning" };
            PositionBeforeMount(warning, actorRoot, site);
            actorRoot.AddChild(warning);
            _arrivalWarnings.Add(warning);
        }
        _arrivalRemaining = ArrivalSeconds;
        IsArrivalWarningActive = true;
    }

    private bool OwnsArrival(int batch, ulong generation) =>
        GodotObject.IsInstanceValid(this) && IsInsideTree() && !IsQueuedForDeletion() && !_retiring &&
        generation == _lifecycleGeneration && IsActive && !IsComplete &&
        batch >= 0 && CurrentBatch == batch && _actors.Count == 0;

    private void RetrySuppressedArrival(double delta)
    {
        int batch = CurrentBatch;
        ulong generation = _lifecycleGeneration;
        if (!OwnsArrival(batch, generation) || !SpawnSuppressed || IsArrivalWarningActive || _batchSpawnPending)
            return;
        _suppressedRetryRemaining -= delta;
        if (_suppressedRetryRemaining > 0d) return;
        // Never catch up with multiple queries in one physics callback. Invalid
        // sites remain quiet; candidate scene/native work runs only on this cadence.
        _suppressedRetryRemaining = SuppressedRetrySeconds;
        Node2D player = CurrentAdmittedPlayer();
        Node actorRoot = GetNodeOrNull(ActorRootPath);
        if (player == null || !GodotObject.IsInstanceValid(actorRoot) ||
            !actorRoot.IsInsideTree() || actorRoot.IsQueuedForDeletion())
        {
            RetireLiveActors();
            return;
        }
        CanyonEnemyPlacement[] placements = ResolveDefinition().Enemies.Where(enemy =>
            enemy.ActivationBatch == batch).ToArray();
#if RANGED_SPAWN_IDENTITY
        var retryIdentity = Knight.Tests.Integration.RangedSpawnIdentityTrace.GroupEdge(this,
            "retry", "begin", batch, generation, _actors.Count);
#endif
        bool legal = ResolveWholeArrival(placements, 0, player,
            new List<(CanyonEnemyPlacement Placement, Vector2 Position)>(), new List<Rect2>());
#if RANGED_SPAWN_IDENTITY
        Knight.Tests.Integration.RangedSpawnIdentityTrace.GroupReturned(retryIdentity,
            CurrentBatch, _lifecycleGeneration, _actors.Count);
#endif
        if (!legal || !OwnsArrival(batch, generation) || !SpawnSuppressed ||
            IsArrivalWarningActive || _batchSpawnPending ||
            !ReferenceEquals(CurrentAdmittedPlayer(), player) ||
            !GodotObject.IsInstanceValid(actorRoot) || !actorRoot.IsInsideTree() ||
            actorRoot.IsQueuedForDeletion() || !ReferenceEquals(GetNodeOrNull(ActorRootPath), actorRoot))
            return;
        // Preflight choices are deliberately discarded. A complete new warning
        // precedes another whole-batch native admission at the actual birth time.
        _batchSpawnPending = true;
        BeginArrival(batch, generation);
    }

    private void FinishArrival()
    {
        int batch = CurrentBatch;
        ulong generation = _lifecycleGeneration;
        if (!IsArrivalWarningActive || !OwnsArrival(batch, generation)) return;
        Node2D player = CurrentAdmittedPlayer();
        if (player == null) { RetireLiveActors(); return; }
        CanyonEnemyPlacement[] placements = ResolveDefinition().Enemies.Where(enemy =>
            enemy.ActivationBatch == batch).ToArray();
        var selected = new List<(CanyonEnemyPlacement Placement, Vector2 Position)>();
#if RANGED_SPAWN_IDENTITY
        var arrivalIdentity = Knight.Tests.Integration.RangedSpawnIdentityTrace.GroupEdge(this,
            "arrival", "begin", batch, generation, _actors.Count);
#endif
        bool resolved = ResolveWholeArrival(placements, 0, player, selected, new List<Rect2>());
#if RANGED_SPAWN_IDENTITY
        Knight.Tests.Integration.RangedSpawnIdentityTrace.GroupReturned(arrivalIdentity,
            CurrentBatch, _lifecycleGeneration, _actors.Count);
#endif
        if (!OwnsArrival(batch, generation) || !IsArrivalWarningActive) return;
        ClearArrivalWarnings();
        if (resolved)
        {
            SpawnSuppressed = false;
            _suppressedRetryRemaining = 0d;
            Node actorRoot = GetNode(ActorRootPath);
            foreach (var arrival in selected)
                SpawnPlacement(actorRoot, arrival.Placement, batch, arrival.Position);
        }
        else
        {
            SpawnSuppressed = true;
            _suppressedRetryRemaining = SuppressedRetrySeconds;
            EmitSignal(SignalName.ArrivalSuppressed, EncounterId);
        }
    }

    // Resolve all legal bodies before AddChild: native physics has not registered
    // an earlier sibling yet, so reserve its complete bounds during this search.
    private bool ResolveWholeArrival(CanyonEnemyPlacement[] placements, int index, Node2D player,
        List<(CanyonEnemyPlacement Placement, Vector2 Position)> selected, List<Rect2> reserved)
    {
        if (index == placements.Length) return placements.Length > 0;
        CanyonEnemyPlacement placement = placements[index];
        foreach (Vector2 site in CandidateSites(placement)
                     .OrderByDescending(site => site.DistanceSquaredTo(player.GlobalPosition)))
        {
            if (!ValidArrivalSite(placement.ScenePath, site, player, out Rect2 body)) continue;
            if (reserved.Any(other => other.Intersects(body))) continue;
            selected.Add((placement, site));
            reserved.Add(body);
            if (ResolveWholeArrival(placements, index + 1, player, selected, reserved)) return true;
            selected.RemoveAt(selected.Count - 1);
            reserved.RemoveAt(reserved.Count - 1);
        }
        return false;
    }

    private static IEnumerable<Vector2> CandidateSites(CanyonEnemyPlacement placement)
    {
        yield return placement.WorldPosition;
        if (placement.AlternateWorldPosition is Vector2 alternate && alternate != placement.WorldPosition)
            yield return alternate;
    }

    private Node2D CurrentAdmittedPlayer()
    {
        if (!GodotObject.IsInstanceValid(this) || !IsInsideTree() || IsQueuedForDeletion()) return null;
        Node2D player = GetTree()?.GetFirstNodeInGroup("player") as Node2D;
        if (!GodotObject.IsInstanceValid(player) || player.IsQueuedForDeletion() ||
            player.GetNodeOrNull<HealthComponent>("Health") is { CurrentHealth: <= 0 }) return null;
        CanyonEncounterDefinition definition = ResolveDefinition();
        return definition.ArenaBounds.HasPoint(player.GlobalPosition) ||
               InsideGearGalleryContinuation(definition, player.GlobalPosition) ||
               SupportedFrozenLeftOverhang(definition, player) ? player : null;
    }

    private static void PositionBeforeMount(Node2D actor, Node actorRoot, Vector2 worldPosition)
    {
        Node ancestor = actorRoot;
        while (ancestor != null && ancestor is not CanvasItem) ancestor = ancestor.GetParent();
        actor.Position = ancestor is CanvasItem canvas
            ? canvas.GetGlobalTransform().AffineInverse() * worldPosition : worldPosition;
    }

    private bool ValidArrivalSite(string scenePath, Vector2 site, Node2D player, out Rect2 reservedBody)
    {
        reservedBody = default;
        CanyonEncounterDefinition definition = ResolveDefinition();
        if (!float.IsFinite(site.X) || !float.IsFinite(site.Y) ||
            !definition.ArenaBounds.HasPoint(site))
            return false;
        PackedScene scene = GD.Load<PackedScene>(scenePath);
#if RANGED_SPAWN_IDENTITY
        var preflightIdentity = Knight.Tests.Integration.RangedSpawnIdentityTrace.Begin(this,
            "preflight", scenePath, scene, CurrentBatch, _lifecycleGeneration);
#endif
        CharacterBody2D candidate = scene?.Instantiate<CharacterBody2D>();
#if RANGED_SPAWN_IDENTITY
        preflightIdentity = Knight.Tests.Integration.RangedSpawnIdentityTrace.Returned(preflightIdentity, candidate);
#endif
        if (candidate == null) return false;
        try
        {
            CollisionShape2D collider = candidate.GetNodeOrNull<CollisionShape2D>("BodyCollider");
            if (collider?.Shape == null) return false;
            Vector2 extent = collider.Shape switch
            {
                CapsuleShape2D capsule => new Vector2(capsule.Radius, capsule.Height * 0.5f),
                RectangleShape2D rectangle => rectangle.Size * 0.5f,
                _ => Vector2.Zero
            };
            if (extent.X <= 0f || extent.Y <= 0f) return false;
            Vector2 center = site + collider.Position;
            Rect2 body = new(center - extent, extent * 2f);
            if (!definition.ArenaBounds.Encloses(body) ||
                CanyonEncounterCatalog.ArrivalBodyOverlapsRest(body)) return false;
            var space = GetViewport().World2D.DirectSpaceState;
            using var shapeQuery = new PhysicsShapeQueryParameters2D
            {
                Shape = collider.Shape, Transform = new Transform2D(0f, center + Vector2.Up * 0.5f),
                CollisionMask = CombatCollisionLayers.World | CombatCollisionLayers.Enemy,
                CollideWithBodies = true, CollideWithAreas = false, Margin = 0f
            };
            using Godot.Collections.Array hits = (Godot.Collections.Array)space.IntersectShape(shapeQuery, 8);
            if (hits.Count > 0) return false;
            Rect2 reservation = body;
            if (!HasContinuousRectangularFloor(body.Position.X, body.End.X, body.End.Y) &&
                !TryCounterweightRustSettlement(scenePath, site, body, collider, space, out reservation))
                return false;
            if (!definition.ArenaBounds.Encloses(reservation) ||
                CanyonEncounterCatalog.ArrivalBodyOverlapsRest(reservation) ||
                !PlayerClearsArrivalBounds(player, reservation)) return false;
            reservedBody = reservation;
            return true;
        }
        finally
        {
#if RANGED_SPAWN_IDENTITY
            Knight.Tests.Integration.RangedSpawnIdentityTrace.FreeEdge(preflightIdentity, "begin");
#endif
            candidate.Free();
#if RANGED_SPAWN_IDENTITY
            Knight.Tests.Integration.RangedSpawnIdentityTrace.FreeEdge(preflightIdentity, "returned");
#endif
        }
    }

    // Only this unchanged authored Rust root sits 4px above its known landing.
    // Admit a clear native fall, never move the actor here or widen other floors.
    private bool TryCounterweightRustSettlement(string scenePath, Vector2 site, Rect2 body,
        CollisionShape2D collider, PhysicsDirectSpaceState2D space, out Rect2 reservation)
    {
        reservation = body;
        CanyonEncounterDefinition definition = ResolveDefinition();
        if (definition.StableId != "canyon.v2.encounter.counterweight-lift" ||
            scenePath != CanyonEncounterCatalog.RustShellScene ||
            !definition.Enemies.Any(enemy => enemy.ScenePath == scenePath && enemy.ActivationBatch == 1 &&
                CandidateSites(enemy).Contains(site))) return false;
        StaticBody2D landing = GetNodeOrNull<StaticBody2D>("../../Collision/LiftBackLanding");
        if (landing == null || (landing.CollisionLayer & CombatCollisionLayers.World) == 0) return false;
        CollisionShape2D floor = Children(landing).OfType<CollisionShape2D>()
            .FirstOrDefault(shape => !shape.Disabled && shape.Shape is RectangleShape2D);
        if (floor?.Shape is not RectangleShape2D rectangle) return false;
        Vector2 half = rectangle.Size * .5f;
        Vector2 left = floor.ToGlobal(-half);
        Vector2 right = floor.ToGlobal(new Vector2(half.X, -half.Y));
        Vector2 bottomLeft = floor.ToGlobal(new Vector2(-half.X, half.Y));
        float drop = left.Y - body.End.Y;
        if (!float.IsFinite(drop) || drop <= 0f || drop > 4f ||
            MathF.Abs(left.Y - right.Y) > .05f || MathF.Abs(left.X - bottomLeft.X) > .05f ||
            body.Position.X < MathF.Min(left.X, right.X) || body.End.X > MathF.Max(left.X, right.X) ||
            !HasContinuousRectangularFloor(body.Position.X, body.End.X, left.Y)) return false;

        // Expected final contact with this supporting floor is excluded only
        // from the swept capsule path; every other World/enemy body still blocks.
        var excluded = new Godot.Collections.Array<Rid> { landing.GetRid() };
        using Godot.Collections.Array excludedOwner = (Godot.Collections.Array)excluded;
        using var query = new PhysicsShapeQueryParameters2D { Shape = collider.Shape,
            Transform = new Transform2D(0f, body.GetCenter() + Vector2.Up * .5f),
            Motion = Vector2.Down * (drop + .5f), Margin = 0f,
            CollisionMask = CombatCollisionLayers.World | CombatCollisionLayers.Enemy,
            CollideWithBodies = true, CollideWithAreas = false, Exclude = excluded };
        float[] travel = space.CastMotion(query);
        if (travel == null || travel.Length < 2 || !float.IsFinite(travel[0]) || !float.IsFinite(travel[1]) ||
            travel[0] != 1f || travel[1] != 1f) return false;
        query.Motion = Vector2.Zero;
        query.Transform = new Transform2D(0f, body.GetCenter() + Vector2.Down * drop);
        using Godot.Collections.Array finalHits = (Godot.Collections.Array)space.IntersectShape(query, 8);
        if (finalHits.Count > 0) return false;
        reservation = new Rect2(body.Position, body.Size + Vector2.Down * drop);
        return true;
    }

    private static bool PlayerClearsArrivalBounds(Node2D player, Rect2 body)
    {
        if (player is not CharacterBody2D playerBody) return true;
        CollisionShape2D collider = playerBody.GetNodeOrNull<CollisionShape2D>("BodyCollider") ??
            Children(playerBody).OfType<CollisionShape2D>().FirstOrDefault();
        Vector2 extent = collider?.Shape switch
        {
            CapsuleShape2D capsule => new Vector2(capsule.Radius, capsule.Height * .5f),
            RectangleShape2D rectangle => rectangle.Size * .5f,
            _ => new Vector2(20f, 36f)
        };
        Vector2 center = player.GlobalPosition + (collider?.Position ?? Vector2.Zero);
        Vector2 bodyCenter = body.GetCenter();
        Vector2 bodyExtent = body.Size * .5f;
        bool verticalOverlap = MathF.Abs(center.Y - bodyCenter.Y) < extent.Y + bodyExtent.Y;
        if (verticalOverlap && MathF.Abs(center.X - bodyCenter.X) < 64f) return false;
        return MathF.Abs(center.X - bodyCenter.X) >= extent.X + bodyExtent.X ||
               MathF.Abs(center.Y - bodyCenter.Y) >= extent.Y + bodyExtent.Y;
    }

    // This canyon's authored ground uses axis-aligned World-layer rectangle shapes. Merge
    // their exact top-edge intervals; edge/center ray samples can bridge an unseen gap.
    private bool HasContinuousRectangularFloor(float left, float right, float foot)
    {
        var intervals = new List<(float Left, float Right)>();
        CollectFloorTopIntervals(GetTree().Root, foot, intervals);
        intervals.Sort((a, b) => a.Left.CompareTo(b.Left));
        float covered = left;
        foreach ((float start, float end) in intervals)
        {
            if (end <= covered) continue;
            if (start > covered + 0.05f) return false;
            covered = end;
            if (covered >= right - 0.05f) return true;
        }
        return false;
    }

    private static void CollectFloorTopIntervals(Node node, float foot,
        List<(float Left, float Right)> intervals)
    {
        if (node is StaticBody2D body &&
            (body.CollisionLayer & CombatCollisionLayers.World) != 0)
        {
            foreach (Node child in Children(body))
            {
                if (child is not CollisionShape2D { Disabled: false, Shape: RectangleShape2D shape }
                    collider) continue;
                Vector2 half = shape.Size * 0.5f;
                Vector2 topLeft = collider.ToGlobal(-half);
                Vector2 topRight = collider.ToGlobal(new Vector2(half.X, -half.Y));
                Vector2 bottomLeft = collider.ToGlobal(new Vector2(-half.X, half.Y));
                if (MathF.Abs(topLeft.Y - topRight.Y) > 0.05f ||
                    MathF.Abs(topLeft.X - bottomLeft.X) > 0.05f ||
                    MathF.Abs(topLeft.Y - foot) > 1.25f) continue;
                intervals.Add((MathF.Min(topLeft.X, topRight.X),
                    MathF.Max(topLeft.X, topRight.X)));
            }
        }
        foreach (Node child in Children(node))
            CollectFloorTopIntervals(child, foot, intervals);
    }

    private static Node[] Children(Node node)
    {
        using Godot.Collections.Array children = (Godot.Collections.Array)node.GetChildren();
        var snapshot = new Node[children.Count];
        for (int i = 0; i < children.Count; i++) snapshot[i] = (Node)children[i].AsGodotObject();
        return snapshot;
    }

    private void ClearArrivalWarnings()
    {
        IsArrivalWarningActive = false;
        _arrivalRemaining = 0d;
        foreach (CanyonSpawnWarningVisual warning in _arrivalWarnings)
            if (GodotObject.IsInstanceValid(warning)) warning.QueueFree();
        _arrivalWarnings.Clear();
    }

    private bool SupportedFrozenLeftOverhang(CanyonEncounterDefinition definition, Node2D player)
    {
        if (definition.StableId != "canyon.v2.encounter.frozen-upper" ||
            player is not CharacterBody2D body || !body.IsOnFloor() ||
            player.GlobalPosition.Y < definition.ArenaBounds.Position.Y ||
            player.GlobalPosition.Y >= definition.ArenaBounds.End.Y ||
            player.GlobalPosition.X >= definition.ArenaBounds.Position.X)
            return false;
        CollisionShape2D collider = body.GetNodeOrNull<CollisionShape2D>("BodyCollider");
        float radius = collider?.Shape is CapsuleShape2D capsule ? capsule.Radius : 0f;
        return radius > 0f && player.GlobalPosition.X + collider.Position.X + radius >=
            definition.ArenaBounds.Position.X;
    }

    // The counterweight's first-gallery transfer briefly leaves the original
    // lower combat arena. Keep that live encounter across the airborne arc and
    // supported landing, without activating it here or extending it into Steam.
    private static bool InsideGearGalleryContinuation(CanyonEncounterDefinition definition,
        Vector2 position) => definition.StableId == "canyon.v2.encounter.gear-pistons" &&
        new Rect2(3040f, 3600f, 240f, 196f).HasPoint(position);

    private void CompleteFromDefeats(CanyonEncounterDefinition definition)
    {
        if (IsComplete) return;
        IsActive = false;
        IsComplete = true;
        _suppressedRetryRemaining = 0d;
        if (definition.GatesProgression)
            _progress?.ActivateMechanism(EncounterId.ToString());
        EmitSignal(SignalName.GroupDefeated, EncounterId);
        EmitSignal(SignalName.Completed, EncounterId);
    }

    private void RetireLiveActors(bool resetBatch = true)
    {
        if (_retiring) return;
        _retiring = true;
#if RANGED_SPAWN_IDENTITY
        var retireIdentity = Knight.Tests.Integration.RangedSpawnIdentityTrace.GroupEdge(this,
            "retire_request", "begin", CurrentBatch, _lifecycleGeneration, _actors.Count);
#endif
        _lifecycleGeneration++;
        _batchSpawnPending = false;
        _suppressedRetryRemaining = 0d;
        ClearArrivalWarnings();
        SpawnSuppressed = false;
        try
        {
            foreach (ActorBinding binding in _actors)
            {
                binding.Actor.Defeated -= binding.Defeated;
                if (!GodotObject.IsInstanceValid(binding.Node)) continue;
                binding.Node.GetParent()?.RemoveChild(binding.Node);
                binding.Node.QueueFree();
            }
            _actors.Clear();
            IsActive = false;
            if (resetBatch) CurrentBatch = -1;
        }
        finally
        {
            _retiring = false;
#if RANGED_SPAWN_IDENTITY
            Knight.Tests.Integration.RangedSpawnIdentityTrace.GroupReturned(retireIdentity,
                CurrentBatch, _lifecycleGeneration, _actors.Count);
#endif
        }
    }

    private CanyonEncounterDefinition ResolveDefinition()
    {
        if (Definition != null) return Definition;
        Definition = CanyonEncounterCatalog.Normal.SingleOrDefault(encounter =>
            encounter.StableId == EncounterId.ToString());
        return Definition;
    }

    private static bool ActivationLessonSatisfied(CanyonEncounterDefinition definition,
        Node2D player) => definition.RouteNode != CanyonRouteNodeId.ControlledAbyss ||
        player is CharacterBody2D body && body.IsOnFloor();

    private sealed record ActorBinding(Node2D Node, IEncounterActor Actor, Action Defeated);
}
