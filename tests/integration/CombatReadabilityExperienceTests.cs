using System;
using System.Collections.Generic;
using System.IO;
using Godot;
using Knight.Combat;
using Knight.Shared;

namespace Knight.Tests.Integration;

/// <summary>
/// Assertions shared by the master runner and the full-route smoke.  The probe only consumes
/// frames published by actors already inside the packed prologue and compares them with the
/// actor's live damage collider; it never manufactures an attack frame.
/// </summary>
public static class CombatReadabilityExperienceTests
{
    public static void Run()
    {
        string smoke = File.ReadAllText(ProjectSettings.GlobalizePath(
            "res://tests/smoke/PrologueCriticalPathSmoke.cs"));
        TestAssert.True(smoke.Contains("new CombatReadabilityExperienceTests.AttackRouteProbe(",
                StringComparison.Ordinal) &&
            smoke.Contains("AttackFrameChanged +=", StringComparison.Ordinal) &&
            smoke.Contains("AssertObserved(", StringComparison.Ordinal),
            "full-route smoke must bind readability probes to real actor AttackFrameChanged publishers");
        TestAssert.True(!smoke.Contains("new AttackPresentationFrame", StringComparison.Ordinal),
            "full-route readability proof cannot manufacture substitute attack frames");
        TestAssert.True(smoke.Contains("AssertDangerPresentationActive", StringComparison.Ordinal) &&
            smoke.Contains("PROLOGUE_ATTACK_VFX_PRECONDITION_OK", StringComparison.Ordinal),
            "full-route cleanup proof must establish nonzero real attack danger/VFX before its boundary");
    }

    public sealed class AttackRouteProbe
    {
        private const float SeparationTolerance = .75f;
        private readonly string _label;
        private readonly Hitbox _hitbox;
        private readonly CollisionShape2D _collider;
        private readonly HashSet<EnemyAttackKind> _activeKinds = new();
        private readonly Dictionary<ulong, AttackRecord> _attacks = new();
        private readonly List<string> _violations = new();

        public AttackRouteProbe(string label, Hitbox hitbox, CollisionShape2D collider)
        {
            _label = label;
            _hitbox = hitbox ?? throw new ArgumentNullException(nameof(hitbox));
            _collider = collider ?? throw new ArgumentNullException(nameof(collider));
        }

        public void Observe(AttackPresentationFrame frame)
        {
            AttackTimelineFrame timeline = frame.Timeline;
            if (timeline.Phase == AttackPhase.None) return;

            if (!_attacks.TryGetValue(timeline.AttackId, out AttackRecord record))
            {
                record = new AttackRecord(timeline.Kind);
                _attacks.Add(timeline.AttackId, record);
            }
            else if (record.Kind != timeline.Kind)
            {
                _violations.Add($"{_label} attack {timeline.AttackId} changed kind " +
                    $"from {record.Kind} to {timeline.Kind}");
            }

            if (timeline.Phase == AttackPhase.Telegraph)
            {
                record.SawTelegraph = true;
                if (timeline.HasLockedTarget)
                {
                    record.SawLockedDanger = true;
                    record.LockedDanger = frame.Shape;
                }
                return;
            }

            if (timeline.Phase != AttackPhase.Active || record.SawActive) return;
            record.SawActive = true;
            _activeKinds.Add(timeline.Kind);
            if (!record.SawTelegraph)
                _violations.Add($"{_label} {timeline.Kind} attack {timeline.AttackId} reached Active before Telegraph");
            if (!record.SawLockedDanger)
                _violations.Add($"{_label} {timeline.Kind} attack {timeline.AttackId} reached Active without a locked true-danger shape");
            if (!_hitbox.Monitoring || _collider.Disabled)
                _violations.Add($"{_label} {timeline.Kind} Active was published while the real damage hitbox was disabled");

            IReadOnlyList<Vector2> activePresentation = ShapePoints(frame.Shape);
            IReadOnlyList<Vector2> activeCollider = ColliderPoints(_collider);
            if (!ConvexPolygonsOverlap(activePresentation, activeCollider))
                _violations.Add($"{_label} {timeline.Kind} Active presentation did not intersect its real damage collider");
            if (record.SawLockedDanger &&
                !ConvexPolygonsOverlap(ShapePoints(record.LockedDanger), activeCollider))
                _violations.Add($"{_label} {timeline.Kind} locked true danger did not intersect its Active hitbox");
        }

        public bool HasActive(EnemyAttackKind kind) => _activeKinds.Contains(kind);

        public void AssertObserved(EnemyAttackKind kind)
        {
            TestAssert.True(_activeKinds.Contains(kind),
                $"{_label} must expose a real {kind} Telegraph before damaging Active; " +
                $"observed=[{string.Join(',', _activeKinds)}]");
            TestAssert.True(_violations.Count == 0,
                $"{_label} readability contract failed: {string.Join("; ", _violations)}");
        }

        private static IReadOnlyList<Vector2> ColliderPoints(CollisionShape2D collider)
        {
            Transform2D transform = collider.GlobalTransform;
            return collider.Shape switch
            {
                RectangleShape2D rectangle => RectanglePoints(transform.Origin,
                    rectangle.Size * .5f, transform.Rotation, transform.X.Length(), transform.Y.Length()),
                CircleShape2D circle => EllipsePoints(transform.Origin,
                    new Vector2(circle.Radius * transform.X.Length(), circle.Radius * transform.Y.Length()),
                    transform.Rotation),
                CapsuleShape2D capsule => CapsulePoints(transform.Origin,
                    new Vector2(capsule.Radius * transform.X.Length(),
                        capsule.Height * .5f * transform.Y.Length()), transform.Rotation),
                ConvexPolygonShape2D polygon => TransformPoints(polygon.Points, transform),
                _ => throw new InvalidOperationException(
                    $"Unsupported live attack collider {collider.Shape?.GetType().Name ?? "null"}")
            };
        }

        private static IReadOnlyList<Vector2> ShapePoints(AttackShapeFrame shape) => shape.Kind switch
        {
            AttackShapeKind.Rectangle or AttackShapeKind.GroundBand =>
                RectanglePoints(shape.Origin, shape.Extent, shape.RotationRadians, 1f, 1f),
            AttackShapeKind.Circle => EllipsePoints(shape.Origin, shape.Extent, shape.RotationRadians),
            AttackShapeKind.Capsule => CapsulePoints(shape.Origin, shape.Extent, shape.RotationRadians),
            AttackShapeKind.ConvexPolygon => shape.Points,
            _ => throw new InvalidOperationException($"Unsupported attack presentation shape {shape.Kind}")
        };

        private static IReadOnlyList<Vector2> RectanglePoints(Vector2 origin, Vector2 halfExtent,
            float rotation, float scaleX, float scaleY)
        {
            Vector2 scaled = new(halfExtent.X * scaleX, halfExtent.Y * scaleY);
            return TransformLocalPoints(origin, rotation,
            [
                new Vector2(-scaled.X, -scaled.Y), new Vector2(scaled.X, -scaled.Y),
                new Vector2(scaled.X, scaled.Y), new Vector2(-scaled.X, scaled.Y)
            ]);
        }

        private static IReadOnlyList<Vector2> EllipsePoints(Vector2 origin, Vector2 radius, float rotation)
        {
            var local = new Vector2[24];
            for (int index = 0; index < local.Length; index++)
            {
                float angle = Mathf.Tau * index / local.Length;
                local[index] = new Vector2(Mathf.Cos(angle) * radius.X, Mathf.Sin(angle) * radius.Y);
            }
            return TransformLocalPoints(origin, rotation, local);
        }

        private static IReadOnlyList<Vector2> CapsulePoints(Vector2 origin, Vector2 extent, float rotation)
        {
            float radius = extent.X;
            float lineHalfLength = Mathf.Max(0f, extent.Y - radius);
            var local = new Vector2[26];
            for (int index = 0; index < 13; index++)
            {
                float angle = Mathf.Pi * index / 12f;
                local[index] = new Vector2(Mathf.Cos(angle) * radius,
                    -lineHalfLength - Mathf.Sin(angle) * radius);
                local[index + 13] = new Vector2(-Mathf.Cos(angle) * radius,
                    lineHalfLength + Mathf.Sin(angle) * radius);
            }
            return TransformLocalPoints(origin, rotation, local);
        }

        private static IReadOnlyList<Vector2> TransformPoints(Vector2[] local, Transform2D transform)
        {
            var world = new Vector2[local.Length];
            for (int index = 0; index < world.Length; index++) world[index] = transform * local[index];
            return world;
        }

        private static IReadOnlyList<Vector2> TransformLocalPoints(Vector2 origin, float rotation,
            IReadOnlyList<Vector2> local)
        {
            var world = new Vector2[local.Count];
            for (int index = 0; index < world.Length; index++)
                world[index] = origin + local[index].Rotated(rotation);
            return world;
        }

        private static bool ConvexPolygonsOverlap(IReadOnlyList<Vector2> first,
            IReadOnlyList<Vector2> second) =>
            first.Count >= 3 && second.Count >= 3 &&
            !HasSeparatingAxis(first, second) && !HasSeparatingAxis(second, first);

        private static bool HasSeparatingAxis(IReadOnlyList<Vector2> axesFrom,
            IReadOnlyList<Vector2> projected)
        {
            for (int index = 0; index < axesFrom.Count; index++)
            {
                Vector2 edge = axesFrom[(index + 1) % axesFrom.Count] - axesFrom[index];
                Vector2 axis = new(-edge.Y, edge.X);
                if (axis.IsZeroApprox()) continue;
                axis = axis.Normalized();
                Project(axesFrom, axis, out float firstMin, out float firstMax);
                Project(projected, axis, out float secondMin, out float secondMax);
                if (firstMax < secondMin - SeparationTolerance ||
                    secondMax < firstMin - SeparationTolerance) return true;
            }
            return false;
        }

        private static void Project(IReadOnlyList<Vector2> points, Vector2 axis,
            out float minimum, out float maximum)
        {
            minimum = maximum = points[0].Dot(axis);
            for (int index = 1; index < points.Count; index++)
            {
                float projection = points[index].Dot(axis);
                minimum = Mathf.Min(minimum, projection);
                maximum = Mathf.Max(maximum, projection);
            }
        }

        private sealed class AttackRecord
        {
            public AttackRecord(EnemyAttackKind kind) => Kind = kind;
            public EnemyAttackKind Kind { get; }
            public bool SawTelegraph { get; set; }
            public bool SawLockedDanger { get; set; }
            public bool SawActive { get; set; }
            public AttackShapeFrame LockedDanger { get; set; }
        }
    }
}
