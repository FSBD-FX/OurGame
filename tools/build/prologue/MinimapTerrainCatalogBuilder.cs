using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Godot;
using Knight.Presentation.Minimap;
using Knight.Prologue;
using Knight.Prologue.Seamless;

namespace Knight.Build.Prologue;

public static class MinimapTerrainCatalogBuilder
{
    public const string StructuralMarker = "minimap_structural_provenance";
    public const string Provenance = "CanyonContentBuilder.Solid/v1";
    private readonly record struct Entry(int Sector, string Path, Vector2 Offset, MinimapSolid Solid);

    public static MinimapTerrainCatalog Build(IReadOnlyList<PrologueSectorDefinition> definitions)
    {
        if (definitions == null || definitions.Count == 0) throw new ArgumentException("Sector definitions required.");
        var entries = new List<Entry>();
        var ids = new HashSet<PrologueSectorId>();
        Rect2 envelope = default;
        foreach (var definition in definitions.OrderBy(d => d?.Id))
        {
            if (definition == null || !ids.Add(definition.Id) || definition.Validate().Count != 0)
                throw new ArgumentException("Invalid or duplicate sector definition.");
            envelope = ids.Count == 1 ? definition.WorldBounds : envelope.Merge(definition.WorldBounds);
            Node instance = definition.Scene.Instantiate();
            try
            {
                if (instance is not PrologueSector sector || sector.ValidateContract(definition).Count != 0 || sector.TopLevel ||
                    sector.Transform.X != Vector2.Right || sector.Transform.Y != Vector2.Down)
                    throw new ArgumentException("Invalid saved sector root.");
                // Runtime overwrites root Position, rather than adding the authored translation.
                var rootTransform = new Transform2D(0, definition.WorldOffset);
                Visit(sector, sector, rootTransform, definition, entries);
            }
            finally { instance.Free(); }
        }
        entries.Sort((a, b) => a.Sector != b.Sector ? a.Sector.CompareTo(b.Sector) : StringComparer.Ordinal.Compare(a.Path, b.Path));
        if (entries.Count == 0 || entries.Count > MinimapTerrainCatalog.MaximumBits)
            throw new ArgumentException("Empty or over-capacity structural geometry.");
        var solids = entries.Select(e => e.Solid).ToArray();
        var boundaries = new List<MinimapBoundary>();
        for (int i = 0; i < solids.Length; i++)
        {
            Rect2 r = solids[i].LocalBounds;
            Face(i, new(r.Position.X, r.Position.Y), new(r.Position.X, r.End.Y), Vector2.Left);
            Face(i, new(r.End.X, r.Position.Y), new(r.End.X, r.End.Y), Vector2.Right);
            Face(i, new(r.Position.X, r.Position.Y), new(r.End.X, r.Position.Y), Vector2.Up);
            Face(i, new(r.Position.X, r.End.Y), new(r.End.X, r.End.Y), Vector2.Down);
        }
        // Canonical binary digest: schema/grid/envelope/sector/path/offset/affine/rect/face/segment order.
        using var stream = new MemoryStream(); using var writer = new BinaryWriter(stream);
        writer.Write(1); writer.Write(8); Vector(envelope.Position); Vector(envelope.Size); writer.Write(entries.Count);
        foreach (Entry e in entries)
        {
            writer.Write(e.Sector); writer.Write(e.Path); Vector(e.Offset);
            Vector(e.Solid.LocalToWorld.X); Vector(e.Solid.LocalToWorld.Y); Vector(e.Solid.LocalToWorld.Origin);
            Vector(e.Solid.LocalBounds.Position); Vector(e.Solid.LocalBounds.Size);
        }
        writer.Write(boundaries.Count);
        foreach (var b in boundaries) { writer.Write(b.ShapeId); Vector(b.FromLocal); Vector(b.ToLocal); Vector(b.OutwardLocal); }
        return new MinimapTerrainCatalog(envelope, Convert.ToHexString(SHA256.HashData(stream.ToArray())), solids, boundaries.ToArray());

        void Vector(Vector2 v) { writer.Write(v.X); writer.Write(v.Y); }
        void Face(int shape, Vector2 a, Vector2 b, Vector2 normal)
        {
            // Use doubles for true transformed length, with slack in subdivision rather than geometry rounding.
            Transform2D t = solids[shape].LocalToWorld;
            Vector2 d = b - a;
            double x = (double)t.X.X * d.X + (double)t.Y.X * d.Y;
            double y = (double)t.X.Y * d.X + (double)t.Y.Y * d.Y;
            double length = Math.Sqrt(x * x + y * y);
            if (!double.IsFinite(length) || length <= 0 || length / 8 > MinimapTerrainCatalog.MaximumBits)
                throw new ArgumentException("Unsupported face extent.");
            int count = Math.Max(1, (int)Math.Ceiling(length / 7.999));
            if (boundaries.Count + (long)count > MinimapTerrainCatalog.MaximumBits)
                throw new ArgumentException("Boundary capacity exceeded; no IDs reused.");
            for (int segment = 0; segment < count; segment++)
                boundaries.Add(new(shape, segment == 0 ? a : a.Lerp(b, (float)segment / count),
                    segment + 1 == count ? b : a.Lerp(b, (float)(segment + 1) / count), normal));
        }
    }

    private static void Visit(Node node, PrologueSector root, Transform2D inherited,
        PrologueSectorDefinition definition, List<Entry> entries)
    {
        if (node != root && node is CanvasLayer) throw new ArgumentException("Canvas inheritance break in sector.");
        Transform2D transform = inherited;
        if (node != root && node is Node2D n)
        {
            if (n.TopLevel || !MinimapTerrainCatalog.Valid(n.Transform)) throw new ArgumentException("Unsupported Node2D inheritance.");
            transform = inherited * n.Transform;
        }
        if (node.HasMeta(StructuralMarker))
        {
            if (node.GetMeta(StructuralMarker).AsString() != Provenance || node is not StaticBody2D body ||
                body.GetScript().VariantType != Variant.Type.Nil ||
                root.GetNodeOrNull<Node>("Gameplay/Collision") is not Node collision || !collision.IsAncestorOf(body))
                throw new ArgumentException("Invalid structural provenance/location.");
            int shapes = 0;
            ValidatePath(body, root);
            foreach (Node child in body.GetChildren())
                if (child is CollisionShape2D shape)
                {
                    if (shape.Disabled || shape.TopLevel || shape.Shape is not RectangleShape2D rect ||
                        !MinimapTerrainCatalog.Finite(rect.Size) || rect.Size.X <= 0 || rect.Size.Y <= 0)
                        throw new ArgumentException("Unsupported structural shape.");
                    // Match fog's body.GlobalTransform * ShapeOwnerGetTransform grouping exactly.
                    var solid = new MinimapSolid(transform * shape.Transform, new Rect2(-rect.Size / 2, rect.Size));
                    entries.Add(new((int)definition.Id, root.GetPathTo(shape).ToString(), definition.WorldOffset, solid));
                    shapes++;
                }
            if (shapes == 0) throw new ArgumentException("Structural body has no rectangle.");
            int nativeShapes = 0;
            foreach (uint owner in body.GetShapeOwners()) nativeShapes += body.ShapeOwnerGetShapeCount(owner);
            if (nativeShapes != shapes) throw new ArgumentException("Ambiguous structural native shape ownership.");
        }
        foreach (Node child in node.GetChildren()) Visit(child, root, transform, definition, entries);
    }

    private static void ValidatePath(Node2D target, PrologueSector root)
    {
        for (Node link = target; link != root; link = link.GetParent())
        {
            if (link is not Node2D node || node.TopLevel || !MinimapTerrainCatalog.Valid(node.Transform))
                throw new ArgumentException("Unsupported target transform inheritance.");
            // The target's own script has a separate contract (e.g. CheckpointTrigger).
            if (link != target && link.GetScript().VariantType != Variant.Type.Nil)
                throw new ArgumentException("Scripted target transform ancestor is ambiguous.");
        }
    }

    public static Godot.Collections.Dictionary<string, Vector2> Checkpoints(IReadOnlyList<PrologueSectorDefinition> definitions)
    {
        var result = new Godot.Collections.Dictionary<string, Vector2>();
        foreach (var d in definitions)
        {
            var root = d.Scene.Instantiate<PrologueSector>();
            try
            {
                if (d.Id == PrologueSectorId.CanyonEntrance)
                {
                    var spawn = root.GetNodeOrNull<Node2D>("Spawn") ?? throw new ArgumentException("Missing authored Spawn.");
                    ValidatePath(spawn, root);
                    result.Add("canyon.v2.start", new Transform2D(0, d.WorldOffset) * spawn.Position);
                }
                Walk(root, new Transform2D(0, d.WorldOffset));
                void Walk(Node node, Transform2D parent)
                {
                    Transform2D t = node == root || node is not Node2D n ? parent : parent * n.Transform;
                    if (node is CheckpointTrigger cp)
                    {
                        ValidatePath(cp, root);
                        if (string.IsNullOrWhiteSpace(cp.EntranceId) || result.ContainsKey(cp.EntranceId))
                            throw new ArgumentException("Invalid authored checkpoint identity.");
                        result.Add(cp.EntranceId, t.Origin);
                    }
                    foreach (Node child in node.GetChildren()) Walk(child, t);
                }
            }
            finally { root.Free(); }
        }
        return result;
    }
}
