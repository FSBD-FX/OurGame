using System;
using System.Collections.Generic;
using Godot;
using Knight.Combat;

namespace Knight.Mechanisms;

public partial class CanyonArcGear : CanyonFixedGear
{
    [Export] public Vector2 PivotOffset { get; set; } = new(80, -60);
    [Export] public float SwingRadians { get; set; } = -1.8545904f;
    [Export] public float PeriodSeconds { get; set; } = 3.2f;
    public ArcGearMotionFrame CurrentMotion => _motion?.Current ?? default;
    public bool HasValidMotionGeometry => _ready && _motion != null && _queryComplete && GeometryMatches(true) &&
        TranslationOnly(GlobalTransform) && Position.DistanceSquaredTo(_origin + CurrentMotion.Offset) < .000001f &&
        GlobalPosition.DistanceSquaredTo(_originWorld + CurrentMotion.Offset) < .000001f &&
        PhysicsServer2D.AreaGetTransform(GetRid()).IsEqualApprox(GlobalTransform);
    public override bool IsDangerTelegraphActive => _query != null && IsPhysicsProcessing() &&
        HasValidMotionGeometry && base.IsDangerTelegraphActive;
    public Rect2 MotionWorldBounds => HasValidMotionGeometry
        ? CircularArcSweep.Bounds(_originWorld + _motion.PivotOffset, -_motion.PivotOffset,
            Math.Min(0, _motion.SwingRadians), Math.Max(0, _motion.SwingRadians), DangerRadius) : default;

    private ArcGearMotionModel _motion;
    private PhysicsShapeQueryParameters2D _query;
    private RectangleShape2D _rectangle;
    private Vector2 _origin, _originWorld;
    private bool _ready, _queryComplete;

    public override void _Ready()
    {
        _ready = false;
        var motion = new ArcGearMotionModel(PivotOffset, SwingRadians, PeriodSeconds);
        if (!TranslationOnly(GlobalTransform)) throw new InvalidOperationException("Arc gear requires a unit translation-only source world basis.");
        base._Ready();
        try
        {
            _origin = Position; _originWorld = GlobalPosition; _motion = motion; _queryComplete = true;
            _rectangle = new RectangleShape2D { Size = Vector2.One };
            _query = new PhysicsShapeQueryParameters2D { Shape = _rectangle, CollisionMask = CombatCollisionLayers.Player,
                CollideWithBodies = true, CollideWithAreas = false, Margin = 0, Motion = Vector2.Zero };
            _ready = true; ForceUpdateTransform();
            if (!HasValidMotionGeometry) throw new InvalidOperationException("Arc authored/native circle and pose must agree.");
        }
        catch { _ready = false; _motion = null; DisposeQueries(); throw; }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (IsProcessingGearContacts || !double.IsFinite(delta) || delta <= 0 ||
            !IsDangerTelegraphActive || GetTree().Paused || !CanProcess()) return;
        ArcGearMotionFrame frame = _motion.Step(delta);
        Position = _origin + frame.Offset; ForceUpdateTransform();
        if (!IsDangerTelegraphActive) return;
        Vector2 pivot = _originWorld + _motion.PivotOffset, ray = -_motion.PivotOffset;
        double a = _motion.SwingRadians * frame.MinimumFraction, b = _motion.SwingRadians * frame.MaximumFraction;
        double lo = Math.Min(a, b), hi = Math.Max(a, b);
        try
        {
            Rect2 bounds = CircularArcSweep.Bounds(pivot, ray, lo, hi, DangerRadius);
            float pad = 4 * Math.Max(Math.Max(Ulp(bounds.Position.X), Ulp(bounds.Position.Y)),
                Math.Max(Ulp(bounds.End.X), Ulp(bounds.End.Y)));
            _rectangle.Size = bounds.Size + Vector2.One * pad * 2; // Broadphase-only roundoff guard.
            _query.Transform = new Transform2D(0, bounds.GetCenter());
            using Godot.Collections.Array hits = (Godot.Collections.Array)GetWorld2D().DirectSpaceState.IntersectShape(_query, 256);
            if (hits.Count >= 256) throw new InvalidOperationException("Arc gear native broadphase truncated; authorization withdrawn.");
            var candidates = new List<CharacterBody2D>(); var seen = new HashSet<ulong>();
            void Candidate(GodotObject value)
            {
                if (value is CharacterBody2D body && Live(body) && body.IsInGroup("player") &&
                    body.GetWorld2D() == GetWorld2D() && seen.Add(body.GetInstanceId())) candidates.Add(body);
            }
            for (int i = 0; i < hits.Count; i++)
            {
                using Godot.Collections.Dictionary hit = hits[i].AsGodotDictionary();
                Candidate(hit["collider"].AsGodotObject());
            }
            // CharacterBody MoveAndSlide/relocation can precede server-state and broadphase publication.
            // Include every registered current Player in this native world, then use exact native shapes
            // at its CURRENT scene pose. This is current-pose completeness, not relative-motion CCD.
            using (Godot.Collections.Array players = (Godot.Collections.Array)GetTree().GetNodesInGroup("player"))
            {
                if (players.Count > 256) throw new InvalidOperationException("Arc current-player enumeration exceeds its complete bound.");
                for (int i = 0; i < players.Count; i++) Candidate(players[i].AsGodotObject());
            }
            var contacts = new List<(CharacterBody2D Body, Transform2D Pose)>();
            foreach (var body in candidates)
                if (Touches(body, pivot, ray, lo, hi)) contacts.Add((body, body.GlobalTransform));
            IEnumerable<Node2D> CurrentContacts()
            {
                foreach (var target in contacts)
                {
                    if (CurrentMotion != frame || !IsDangerTelegraphActive) yield break;
                    // A callback may legitimately relocate another Player before its native pose commits.
                    // That revokes this old batch grant; never damage it or force the physics server to catch up.
                    if (!Live(target.Body) || target.Body.GlobalTransform != target.Pose) continue;
                    if (Touches(target.Body, pivot, ray, lo, hi)) yield return target.Body;
                }
            }
            ProcessGearContacts(CurrentContacts(), delta);
        }
        catch { _queryComplete = false; throw; }
    }

    private bool Touches(CharacterBody2D body, Vector2 pivot, Vector2 ray, double lo, double hi)
    {
        if (!Live(body) || body.GetWorld2D() != GetWorld2D() ||
            (body.CollisionLayer & CombatCollisionLayers.Player) == 0) return false;
        Transform2D currentBody = body.GlobalTransform;
        int count = PhysicsServer2D.BodyGetShapeCount(body.GetRid());
        if (count < 1 || count > 256) throw new InvalidOperationException("Arc target native shape list is not bounded.");
        bool contact = false;
        for (int index = 0; index < count; index++)
        {
            uint owner = body.ShapeFindOwner(index);
            if (body.IsShapeOwnerDisabled(owner)) continue;
            Shape2D shape = null;
            for (int j = 0; j < body.ShapeOwnerGetShapeCount(owner); j++)
                if (body.ShapeOwnerGetShapeIndex(owner, j) == index) { shape = body.ShapeOwnerGetShape(owner, j); break; }
            if (!IsInstanceValid(shape)) throw new InvalidOperationException("Arc target shape owner does not resolve native index.");
            Rid native = PhysicsServer2D.BodyGetShape(body.GetRid(), index);
            Transform2D local = PhysicsServer2D.BodyGetShapeTransform(body.GetRid(), index);
            if (native != shape.GetRid() || !local.IsEqualApprox(body.ShapeOwnerGetTransform(owner)))
                throw new InvalidOperationException("Arc target native shape identity/local transform mismatch.");
            Transform2D world = currentBody * local;
            double scale = SimilarityScale(world);
            using Variant data = PhysicsServer2D.ShapeGetData(native);
            bool hit;
            if (shape is CircleShape2D circle && PhysicsServer2D.ShapeGetType(native) == PhysicsServer2D.ShapeType.Circle &&
                data.VariantType == Variant.Type.Float && Mathf.IsEqualApprox(data.AsSingle(), circle.Radius))
                hit = CircularArcSweep.IntersectsCircle(pivot, ray, lo, hi, DangerRadius, world.Origin, circle.Radius * scale);
            else if (shape is CapsuleShape2D capsule && PhysicsServer2D.ShapeGetType(native) == PhysicsServer2D.ShapeType.Capsule &&
                data.VariantType == Variant.Type.Vector2 && data.AsVector2().IsEqualApprox(new(capsule.Height, capsule.Radius)))
            {
                float half = Math.Max(0, capsule.Height * .5f - capsule.Radius);
                hit = CircularArcSweep.IntersectsCapsule(pivot, ray, lo, hi, DangerRadius,
                    world * new Vector2(0, -half), world * new Vector2(0, half), capsule.Radius * scale);
            }
            else if (shape is RectangleShape2D rectangle && PhysicsServer2D.ShapeGetType(native) == PhysicsServer2D.ShapeType.Rectangle &&
                data.VariantType == Variant.Type.Vector2 && data.AsVector2().IsEqualApprox(rectangle.Size * .5f))
                hit = CircularArcSweep.IntersectsRectangle(pivot, ray, lo, hi, DangerRadius, world, rectangle.Size);
            else throw new InvalidOperationException("Arc target shape unsupported or native dimensions inconsistent.");
            contact |= hit; // Inspect every enabled native shape, even after a hit; no early body dedup masks a later shape.
        }
        return contact;
    }

    private static double SimilarityScale(Transform2D transform)
    {
        if (!transform.Origin.IsFinite() || !transform.X.IsFinite() || !transform.Y.IsFinite())
            throw new InvalidOperationException("Arc target transform must be finite.");
        double x = (double)transform.X.X * transform.X.X + (double)transform.X.Y * transform.X.Y;
        double y = (double)transform.Y.X * transform.Y.X + (double)transform.Y.Y * transform.Y.Y;
        double dot = (double)transform.X.X * transform.Y.X + (double)transform.X.Y * transform.Y.Y;
        if (x <= 0 || y <= 0 || Math.Abs(x - y) > 1e-6 * Math.Max(x, y) || Math.Abs(dot) > 1e-6 * Math.Sqrt(x * y))
            throw new InvalidOperationException("Arc target circle/capsule basis must be similarity-only, not shear/nonuniform scale.");
        return Math.Sqrt(x);
    }
    private static float Ulp(float x) => Math.Abs(MathF.BitIncrement(x) - x);
    private static bool Live(Node node)
    {
        if (!IsInstanceValid(node) || !node.IsInsideTree()) return false;
        for (Node p = node; p != null; p = p.GetParent()) if (p.IsQueuedForDeletion()) return false;
        return true;
    }
    private static bool TranslationOnly(Transform2D t) => t.Origin.IsFinite() &&
        t.X.DistanceSquaredTo(Vector2.Right) < .00000001f && t.Y.DistanceSquaredTo(Vector2.Down) < .00000001f;
    public override void ResetForCheckpoint()
    {
        base.ResetForCheckpoint();
        if (!_ready || _motion == null) return;
        _motion.Reset(); Position = _origin; ForceUpdateTransform(); QueueRedraw();
    }
    public override void _ExitTree()
    {
        base._ExitTree(); _ready = false;
        if (_motion != null) Position = _origin;
        _motion = null; DisposeQueries(); RequestReady();
    }
    private void DisposeQueries() { _query?.Dispose(); _query = null; _rectangle?.Dispose(); _rectangle = null; }
    public override void _Draw()
    {
        if (!HasValidMotionGeometry) return;
        Vector2 pivot = _motion.PivotOffset - CurrentMotion.Offset;
        DrawLine(pivot, Vector2.Zero, new Color("162329"), 7, true);
        DrawLine(pivot, Vector2.Zero, new Color("6d827c"), 2, true);
        DrawCircle(pivot, 6, new Color("d4aa58"));
        base._Draw();
    }
}
