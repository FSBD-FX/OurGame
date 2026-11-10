using System;
using System.IO;
using Godot;

namespace Knight.Presentation.Minimap;

public readonly record struct MinimapSolid(Transform2D LocalToWorld, Rect2 LocalBounds);
public readonly record struct MinimapBoundary(int ShapeId, Vector2 FromLocal,
    Vector2 ToLocal, Vector2 OutwardLocal);

// Generated catalog storage is added by the bounded-model/build tasks. Geometry
// values deliberately have no node identity, persistence path or physics query.
public partial class MinimapTerrainCatalog : Resource
{
    public const int MaximumBits = 1048576;
    private MinimapSolid[] _solids = Array.Empty<MinimapSolid>();
    private MinimapBoundary[] _boundaries = Array.Empty<MinimapBoundary>();
    private byte[] _serialized = Array.Empty<byte>();
    // A single atomic value avoids serializer property-order dependent empty catalogs.
    // Values are copied and validated before publication; callers cannot mutate frozen geometry.
    [Export] public byte[] SerializedValues
    {
        get => (byte[])_serialized.Clone();
        set
        {
            if (CellCount != 0) throw new InvalidOperationException("Catalog is frozen.");
            if (value == null || value.Length < 33 || value.Length > 80 * MaximumBits)
                throw new ArgumentException("Invalid serialized catalog length.");
            using var stream = new MemoryStream(value, false);
            using var reader = new BinaryReader(stream);
            int schema = reader.ReadInt32();
            if (schema is not (1 or 2)) throw new ArgumentException("Invalid catalog schema.");
            int cellSize = schema == 1 ? 8 : reader.ReadInt32();
            if (schema == 2 && cellSize != 16) throw new ArgumentException("Invalid catalog grid.");
            Rect2 bounds = new(ReadVector(reader), ReadVector(reader));
            int revisionLength = reader.ReadInt32(), solidCount = reader.ReadInt32(), boundaryCount = reader.ReadInt32();
            if (revisionLength is < 1 or > 1024 || solidCount < 0 || solidCount > MaximumBits ||
                boundaryCount < 0 || boundaryCount > MaximumBits ||
                stream.Length != (schema == 1 ? 32L : 36L) + revisionLength + 40L * solidCount + 28L * boundaryCount)
                throw new ArgumentException("Invalid serialized catalog counts.");
            string revision = System.Text.Encoding.UTF8.GetString(reader.ReadBytes(revisionLength));
            var solids = new MinimapSolid[solidCount];
            var boundaries = new MinimapBoundary[boundaryCount];
            for (int i = 0; i < solids.Length; i++)
                solids[i] = new(new Transform2D(ReadVector(reader), ReadVector(reader), ReadVector(reader)),
                    new Rect2(ReadVector(reader), ReadVector(reader)));
            for (int i = 0; i < boundaries.Length; i++)
                boundaries[i] = new(reader.ReadInt32(), ReadVector(reader), ReadVector(reader), ReadVector(reader));
            Configure(bounds, revision, solids, boundaries, cellSize);
            _serialized = (byte[])value.Clone();
        }
    }
    public Rect2 WorldBounds { get; private set; }
    public string Revision { get; private set; } = string.Empty;
    public int Columns { get; private set; }
    public int Rows { get; private set; }
    public int CellCount { get; private set; }
    public int CellSize { get; private set; } = 8;
    public int SolidCount => _solids.Length;
    public int BoundaryCount => _boundaries.Length;
    public MinimapTerrainCatalog() { }
    public MinimapTerrainCatalog(Rect2 bounds, string revision,
        ReadOnlySpan<MinimapSolid> solids = default, ReadOnlySpan<MinimapBoundary> boundaries = default, int cellSize = 8)
    {
        Configure(bounds, revision, solids, boundaries, cellSize);
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        byte[] revisionBytes = System.Text.Encoding.UTF8.GetBytes(revision);
        writer.Write(cellSize == 8 ? 1 : 2);
        if (cellSize != 8) writer.Write(cellSize);
        WriteVector(writer, bounds.Position); WriteVector(writer, bounds.Size);
        writer.Write(revisionBytes.Length); writer.Write(solids.Length); writer.Write(boundaries.Length); writer.Write(revisionBytes);
        foreach (MinimapSolid solid in solids)
        {
            WriteVector(writer, solid.LocalToWorld.X); WriteVector(writer, solid.LocalToWorld.Y); WriteVector(writer, solid.LocalToWorld.Origin);
            WriteVector(writer, solid.LocalBounds.Position); WriteVector(writer, solid.LocalBounds.Size);
        }
        foreach (MinimapBoundary boundary in boundaries)
        {
            writer.Write(boundary.ShapeId); WriteVector(writer, boundary.FromLocal); WriteVector(writer, boundary.ToLocal); WriteVector(writer, boundary.OutwardLocal);
        }
        _serialized = stream.ToArray();
    }
    private void Configure(Rect2 bounds, string revision,
        ReadOnlySpan<MinimapSolid> solids, ReadOnlySpan<MinimapBoundary> boundaries, int cellSize)
    {
        if (!Valid(bounds) || string.IsNullOrWhiteSpace(revision) || System.Text.Encoding.UTF8.GetByteCount(revision) > 1024 ||
            cellSize is not (8 or 16) || bounds.Size.X % cellSize != 0 || bounds.Size.Y % cellSize != 0 ||
            bounds.Size.X / cellSize > MaximumBits || bounds.Size.Y / cellSize > MaximumBits ||
            (double)(bounds.Size.X / cellSize) * (bounds.Size.Y / cellSize) > MaximumBits ||
            boundaries.Length > MaximumBits || solids.Length > MaximumBits)
            throw new ArgumentException("Invalid or over-capacity minimap catalog.");
        foreach (MinimapSolid solid in solids)
            if (!Valid(solid.LocalBounds) || !Valid(solid.LocalToWorld))
                throw new ArgumentException("Invalid minimap solid.");
        foreach (MinimapBoundary boundary in boundaries)
        {
            if ((uint)boundary.ShapeId >= (uint)solids.Length || !Finite(boundary.FromLocal) ||
                !Finite(boundary.ToLocal) || !Finite(boundary.OutwardLocal))
                throw new ArgumentException("Invalid minimap boundary.");
            MinimapSolid solid = solids[boundary.ShapeId];
            Vector2 a = solid.LocalToWorld * boundary.FromLocal, b = solid.LocalToWorld * boundary.ToLocal;
            if (!Finite(a) || !Finite(b) || a == b || a.DistanceSquaredTo(b) > 64.00001f ||
                !Inside(bounds, a) || !Inside(bounds, b)) throw new ArgumentException("Invalid boundary extent.");
        }
        _solids = solids.ToArray(); _boundaries = boundaries.ToArray();
        WorldBounds = bounds; Revision = revision;
        CellSize = cellSize;
        Columns = (int)(bounds.Size.X / cellSize); Rows = (int)(bounds.Size.Y / cellSize);
        CellCount = Columns * Rows;
    }
    private static Vector2 ReadVector(BinaryReader reader) => new(reader.ReadSingle(), reader.ReadSingle());
    private static void WriteVector(BinaryWriter writer, Vector2 value) { writer.Write(value.X); writer.Write(value.Y); }
    public Rect2 Cell(int id)
    {
        if ((uint)id >= (uint)CellCount) throw new ArgumentOutOfRangeException(nameof(id));
        return new Rect2(WorldBounds.Position + new Vector2(id % Columns * CellSize, id / Columns * CellSize), new Vector2(CellSize, CellSize));
    }
    public MinimapSolid Solid(int id) => (uint)id < (uint)_solids.Length ? _solids[id] :
        throw new ArgumentOutOfRangeException(nameof(id));
    public MinimapBoundary Boundary(int id) => (uint)id < (uint)BoundaryCount ? _boundaries[id] :
        throw new ArgumentOutOfRangeException(nameof(id));
    internal static bool Finite(Vector2 v) => float.IsFinite(v.X) && float.IsFinite(v.Y);
    internal static bool Valid(Rect2 r) => Finite(r.Position) && Finite(r.Size) && Finite(r.End) &&
        r.Size.X > 0 && r.Size.Y > 0 && r.End.X > r.Position.X && r.End.Y > r.Position.Y;
    internal static bool Valid(Transform2D t) => Finite(t.X) && Finite(t.Y) && Finite(t.Origin) &&
        Math.Abs((double)t.X.X * t.Y.Y - (double)t.Y.X * t.X.Y) > 1e-12 *
        Math.Max(1, Math.Abs((double)t.X.X * t.Y.Y) + Math.Abs((double)t.Y.X * t.X.Y));
    private static bool Inside(Rect2 r, Vector2 p) => p.X >= r.Position.X && p.Y >= r.Position.Y &&
        p.X <= r.End.X && p.Y <= r.End.Y;
}
