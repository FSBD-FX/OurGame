#nullable enable
using Godot;
using Knight.Combat;
using Knight.Enemies.Sentry;
using Knight.Mechanisms;
using Knight.Prologue;
using Knight.Prologue.Guidance;
using Knight.Prologue.Seamless;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Text.Json;
using System.Threading;

namespace Knight.Tests.Diagnostics;

// Test-runner observation only: never refreshes or changes a producer/fixture.
public sealed class Task17TelegraphFailureObserver : IDisposable
{
    private const string TargetMessage = "Assertion failed: same telegraph publisher outside its active arena cannot suppress firefly";
    private const int NodeLimit = 4096;
    private const int JsonLimit = 262144;
    private readonly SceneTree _tree;
    private int _seen;
    private bool _snapshotEmitted;
    private bool _observationFailed;
    private bool _disposed;

    public Task17TelegraphFailureObserver(SceneTree tree)
    {
        _tree = tree;
        AppDomain.CurrentDomain.FirstChanceException += OnFirstChance;
    }

    private void OnFirstChance(object? sender, FirstChanceExceptionEventArgs args)
    {
        if (args.Exception.GetType() != typeof(InvalidOperationException) ||
            !string.Equals(args.Exception.Message, TargetMessage, StringComparison.Ordinal) ||
            Interlocked.CompareExchange(ref _seen, 1, 0) != 0) return;
        // Claim the single occurrence before any reads/logging, preventing recursion
        // and repeated notifications as the same async exception propagates.
        try
        {
            string json = JsonSerializer.Serialize(Snapshot());
            if (json.Length > JsonLimit)
                throw new InvalidOperationException("Diagnostic snapshot exceeded its explicit JSON bound.");
            Console.WriteLine("TASK17B_REMOTE_ORIGIN " + json);
            _snapshotEmitted = true;
        }
        catch (Exception observationError)
        {
            _observationFailed = true;
            EmitFailure(observationError);
        }
        // No throw/rethrow here: the original assertion continues naturally.
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        AppDomain.CurrentDomain.FirstChanceException -= OnFirstChance;
        try
        {
            Console.WriteLine("TASK17B_REMOTE_ORIGIN " + JsonSerializer.Serialize(new
            {
                status = "scope_closed", targetObserved = _seen != 0,
                snapshotEmitted = _snapshotEmitted, observationFailed = _observationFailed,
                unsubscribed = true, acceptance = "observational_only"
            }));
        }
        catch (Exception observationError) { EmitFailure(observationError); }
    }

    private static void EmitFailure(Exception error)
    {
        // A failed observer/log sink must never replace the original assertion.
        // Separate stdout marker plus stderr detail leave failures independently visible.
        try { Console.WriteLine("TASK17B_REMOTE_ORIGIN {\"status\":\"observation_failed\",\"acceptance\":\"observational_only\"}"); }
        catch { /* Try the independent stderr sink below; never mask the assertion. */ }
        try { Console.Error.WriteLine("TASK17B_REMOTE_ORIGIN_OBSERVATION_FAILED " + error); }
        catch { /* Neither sink is available; the original assertion still propagates. */ }
    }

    private object Snapshot()
    {
        ulong physicsFrame = Engine.GetPhysicsFrames();
        ulong processFrame = Engine.GetProcessFrames();
        PrologueLevelController[] levels = Descendants(_tree.Root)
            .OfType<PrologueLevelController>().Where(node => node.IsInsideTree()).ToArray();
        if (levels.Length != 1)
            throw new InvalidOperationException("Diagnostic requires exactly one mounted level; found " + levels.Length);
        PrologueLevelController level = levels[0];
        PrologueSector sector = level.CurrentSectorRoot;
        CharacterBody2D player = level.Player;
        CanyonFireflyGuidanceController guidance = level.GetNode<CanyonFireflyGuidanceController>("WorldRoot/FireflyGuidance");
        PrologueGameFlowController flow = level.GetNode<PrologueGameFlowController>("GameFlow");
        SeamlessPrologueWorld world = level.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors");
        bool resolved = CanyonWorldLayout.TryResolveRouteNode(player.GlobalPosition, out CanyonRouteNodeId routeId);
        Rect2? routeBounds = resolved ? CanyonWorldLayout.Nodes.Single(node => node.Id == routeId).WorldBounds : null;
        Rect2 viewport = level.GetViewportRect();
        Node[] traversal = Descendants(sector).ToArray();
        var mechanisms = new List<object>();
        object? firstMatchingMechanism = null;
        foreach ((Node node, int order) in traversal.Select((node, order) => (node, order)))
        {
            if (node is not ICanyonTelegraphSource source) continue;
            bool danger = source.IsDangerTelegraphActive;
            var rectangles = new List<object>();
            bool localMatch = false;
            foreach (CollisionShape2D collision in Descendants(node).OfType<CollisionShape2D>())
            {
                if (collision.Shape is not RectangleShape2D shape) continue;
                Rect2 worldBounds = RectangleBounds(collision.GlobalTransform, shape.Size);
                Rect2 screenBounds = RectangleBounds(collision.GetGlobalTransformWithCanvas(), shape.Size);
                bool routeIntersects = routeBounds.HasValue && Overlaps(routeBounds.Value, worldBounds);
                bool viewportIntersects = Overlaps(viewport, screenBounds);
                localMatch |= routeIntersects && viewportIntersects;
                rectangles.Add(new
                {
                    node = Identity(collision), size = XY(shape.Size), disabled = collision.Disabled,
                    globalTransform = Transform(collision.GlobalTransform),
                    canvasTransform = Transform(collision.GetGlobalTransformWithCanvas()),
                    worldBounds = Rectangle(worldBounds), screenBounds = Rectangle(screenBounds),
                    routeIntersects, viewportIntersects
                });
            }
            bool matches = node.IsInsideTree() && danger && localMatch;
            if (matches && firstMatchingMechanism == null)
                firstMatchingMechanism = new { order, node = Identity(node) };
            object? producer = node switch
            {
                CanyonSteamDiverter diverter => new
                {
                    mechanismId = diverter.MechanismId, selectedSide = diverter.SelectedSide.ToString(),
                    isHandingOff = diverter.IsHandingOff, hasActivated = diverter.HasActivated
                },
                CanyonSteamJet jet => new
                {
                    frame = new { telegraphing = jet.Frame.Telegraphing, hazardActive = jet.Frame.HazardActive,
                        traversable = jet.Frame.Traversable, phase01 = jet.Frame.Phase01 },
                    elapsedSeconds = jet.ElapsedSeconds, disabledForRun = jet.DisabledForRun,
                    safeSeconds = jet.SafeSeconds, telegraphSeconds = jet.TelegraphSeconds,
                    activeSeconds = jet.ActiveSeconds, monitoring = jet.Monitoring
                },
                _ => null
            };
            mechanisms.Add(new { order, node = Identity(node), danger, localMatch, matches, rectangles, producer });
        }
        CanyonEncounterGroup[] groups = Descendants(sector.GetNodeOrNull("Gameplay/Encounters"))
            .OfType<CanyonEncounterGroup>().ToArray();
        object[] encounters = groups.Select(group => (object)new
        {
            node = Identity(group), encounterId = group.EncounterId.ToString(), active = group.IsActive,
            liveActorCount = group.LiveActorCount, hasDefinition = group.Definition != null,
            arena = group.Definition == null ? null : Rectangle(group.Definition.ArenaBounds)
        }).ToArray();
        object[] actors = Descendants(sector.GetNodeOrNull("Gameplay/Actors")).OfType<Node2D>()
            .Select(actor => (object)new
            {
                node = Identity(actor), position = XY(actor.GlobalPosition),
                isEncounterActor = actor is IEncounterActor,
                telegraph = CanyonCombatTelegraphAdapter.IsTelegraphActive(actor),
                arenaMembership = groups.Select(group => new
                {
                    encounterId = group.EncounterId.ToString(), instanceId = group.GetInstanceId(),
                    active = group.IsActive, hasDefinition = group.Definition != null,
                    containsPosition = group.Definition?.ArenaBounds.HasPoint(actor.GlobalPosition) == true
                }).ToArray(),
                sentryTimeline = actor is BellBoltSentry sentry ? Timeline(sentry.CurrentFrame.AttackTimeline) : null
            }).ToArray();
        Marker2D? marker = guidance.CurrentMarker;
        return new
        {
            status = "failure_snapshot", exceptionType = typeof(InvalidOperationException).FullName,
            exceptionMessage = TargetMessage, physicsFrame,
            processFrame, paused = _tree.Paused, flow = flow.State.ToString(),
            level = Identity(level), sector = Identity(sector), sectorId = level.CurrentSector?.ToString(),
            mountedSectorIds = world.MountedSectorIds.Select(id => id.ToString()).ToArray(),
            layoutVersion = world.LayoutVersion, layoutActive = guidance.IsLayoutActive,
            player = new { node = Identity(player), position = XY(player.GlobalPosition) },
            route = new { resolved, id = resolved ? routeId.ToString() : null,
                bounds = routeBounds.HasValue ? Rectangle(routeBounds.Value) : null },
            viewport = Rectangle(viewport),
            firefly = new { combatTelegraphActive = guidance.CombatTelegraphActive,
                visible = guidance.CurrentFrame.Visible, visibleSecondsRemaining = guidance.CurrentFrame.VisibleSecondsRemaining,
                cooldownSecondsRemaining = guidance.CurrentFrame.CooldownSecondsRemaining,
                marker = GodotObject.IsInstanceValid(marker) ? new { node = Identity(marker!),
                    position = XY(marker!.GlobalPosition), screenPosition = XY(marker.GetGlobalTransformWithCanvas().Origin) } : null },
            traversalNodeCount = traversal.Length, firstMatchingMechanism, mechanisms, encounters, actors,
            ownership = "arena membership only; no private actor ownership inference",
            geometry = "independent affine-basis AABB; strict raw route/viewport intersections",
            acceptance = "observational_only"
        };
    }

    private static object Timeline(AttackTimelineFrame frame) => new
    {
        attackId = frame.AttackId, kind = frame.Kind.ToString(), phase = frame.Phase.ToString(),
        phaseDurationSeconds = frame.PhaseDurationSeconds, phaseRemainingSeconds = frame.PhaseRemainingSeconds,
        lockedWorldTarget = XY(frame.LockedWorldTarget), hasLockedTarget = frame.HasLockedTarget
    };

    private static object Identity(Node node) => new
    {
        instanceId = node.GetInstanceId(), path = node.IsInsideTree() ? node.GetPath().ToString() : null,
        name = node.Name.ToString(), type = node.GetType().FullName,
        inTree = node.IsInsideTree(), queuedForDeletion = node.IsQueuedForDeletion()
    };

    private static object XY(Vector2 value) => new { x = value.X, y = value.Y };
    private static object Rectangle(Rect2 value) => new
        { x = value.Position.X, y = value.Position.Y, width = value.Size.X, height = value.Size.Y };
    private static object Transform(Transform2D value) => new
        { x = XY(value.X), y = XY(value.Y), origin = XY(value.Origin) };

    // For a centered rectangle under an affine transform, each projected half
    // extent is the sum of the absolute basis contributions. No resolver call.
    private static Rect2 RectangleBounds(Transform2D transform, Vector2 size)
    {
        float halfX = size.X * .5f, halfY = size.Y * .5f;
        var extent = new Vector2(MathF.Abs(transform.X.X) * halfX + MathF.Abs(transform.Y.X) * halfY,
            MathF.Abs(transform.X.Y) * halfX + MathF.Abs(transform.Y.Y) * halfY);
        return new Rect2(transform.Origin - extent, extent * 2f);
    }

    private static bool Overlaps(Rect2 left, Rect2 right) =>
        left.Position.X < right.End.X && right.Position.X < left.End.X &&
        left.Position.Y < right.End.Y && right.Position.Y < left.End.Y;

    private static IEnumerable<Node> Descendants(Node? root)
    {
        if (!GodotObject.IsInstanceValid(root)) yield break;
        int count = 0;
        var pending = new Stack<Node>();
        foreach (Node child in root!.GetChildren().AsEnumerable().Reverse()) pending.Push(child);
        while (pending.Count != 0)
        {
            if (++count > NodeLimit)
                throw new InvalidOperationException("Diagnostic traversal exceeded its explicit node bound.");
            Node node = pending.Pop();
            yield return node;
            foreach (Node child in node.GetChildren().AsEnumerable().Reverse()) pending.Push(child);
        }
    }
}
