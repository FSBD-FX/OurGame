using System;
using Godot;

namespace Knight.Presentation.Minimap;

public readonly record struct MinimapCheckpointDescriptor(string Id, Vector2 WorldPosition);

// A presentation-only local cache. It never advances the exploration model.
public partial class PrologueMinimapView : Control
{
    public static readonly Vector2 BaseSize = new(260, 170);
    public const float WorldViewWidth = 1280f;
    public const float OuterFeather = 24f;
    public const int CacheWidth = 256, CacheHeight = 192;
    public const int MaximumLocalBoundaries = 8192, MaximumBoundaryVisits = 65536;
    public static readonly Color KnownSpaceColor = new(.12f, .18f, .19f);
    public static readonly Color KnownBoundaryColor = new(.65f, .75f, .73f);
    public int CacheRebuildCount { get; private set; }
    public int TextureUploadCount { get; private set; }
    public string LastCacheReason { get; private set; } = "none";
    public Rect2 CacheWorldRect { get; private set; }
    public Rect2 ViewWorldRect { get; private set; }
    public int LastBoundaryVisits { get; private set; }
    public int LocalBoundaryCount => _boundaryCount;
    public bool BoundaryQueryOverflow { get; private set; }
    public int StaticDirectoryBytes => ((_heads?.Length ?? 0) + (_next?.Length ?? 0)) * sizeof(int);
    public int DiscoveredCheckpointCount { get; private set; }
    public int TextureCreationCount { get; private set; }
    private readonly byte[] _pixels = new byte[CacheWidth * CacheHeight];
    private readonly int[] _localBoundaries = new int[MaximumLocalBoundaries];
    private MinimapCheckpointDescriptor[] _checkpoints = Array.Empty<MinimapCheckpointDescriptor>();
    private bool _configured;
    private Godot.Collections.Dictionary<string, Vector2> _authoredCheckpoints = new();
    [Export] public Godot.Collections.Dictionary<string, Vector2> AuthoredCheckpoints
    {
        get => new(_authoredCheckpoints);
        set
        {
            if (_configured || IsInsideTree()) throw new InvalidOperationException("Authored checkpoints configure before Ready.");
            _authoredCheckpoints = value == null ? new() : new(value);
        }
    }
    private ExplorationMapModel _model;
    private MinimapTerrainCatalog _catalog;
    private ulong _revision;
    private int[] _heads, _next;
    private int _bucketColumns, _bucketRows, _boundaryCount;
    private Image _image;
    private ImageTexture _texture;
    private ColorRect _surface;
    private MinimapBoundaryLayer _boundaryLayer;
    private ShaderMaterial _maskMaterial, _boundaryMaterial;
    private Vector2 _center, _actor, _facing = Vector2.Right;
    private bool _hasCenter;
    private Vector2 _lastSampling = new(float.NaN, float.NaN), _lastWorldSize, _lastSize, _lastActor, _lastFacing;

    public override void _Ready()
    {
        if (!_configured)
        {
            var descriptors = new MinimapCheckpointDescriptor[_authoredCheckpoints.Count]; int i = 0;
            foreach (var item in _authoredCheckpoints) descriptors[i++] = new(item.Key, item.Value);
            ConfigureCheckpoints(descriptors);
        }
        MouseFilter = MouseFilterEnum.Ignore; FocusMode = FocusModeEnum.None;
        TextureFilter = TextureFilterEnum.Nearest;
        ClipContents = true;
        Shader shader = GD.Load<Shader>("res://scripts/presentation/minimap/minimap-mask.gdshader");
        _maskMaterial = new ShaderMaterial { Shader = shader };
        _boundaryMaterial = new ShaderMaterial { Shader = shader };
        _boundaryMaterial.SetShaderParameter("boundary_pass", true);
        _maskMaterial.SetShaderParameter("outer_feather", EffectiveOuterFeather);
        _boundaryMaterial.SetShaderParameter("outer_feather", EffectiveOuterFeather);
        _image = Image.CreateFromData(CacheWidth, CacheHeight, false, Image.Format.R8, _pixels);
        _texture = ImageTexture.CreateFromImage(_image); TextureCreationCount++;
        _maskMaterial.SetShaderParameter("known_mask", _texture);
        _maskMaterial.SetShaderParameter("space_color", KnownSpaceColor);
        _boundaryMaterial.SetShaderParameter("boundary_color", KnownBoundaryColor);
        _surface = new ColorRect { Name = "KnownTerrain", MouseFilter = MouseFilterEnum.Ignore,
            FocusMode = FocusModeEnum.None, Material = _maskMaterial, ShowBehindParent = true };
        _surface.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(_surface);
        _boundaryLayer = new MinimapBoundaryLayer { Name = "KnownSegments", Material = _boundaryMaterial,
            ShowBehindParent = true, View = this };
        AddChild(_boundaryLayer);
        Visible = false;
    }

    public void ConfigureCheckpoints(ReadOnlySpan<MinimapCheckpointDescriptor> descriptors)
    {
        if (_configured || _model != null) throw new InvalidOperationException("Checkpoint descriptors configure once before presentation.");
        if (descriptors.Length > 256) throw new ArgumentException("Checkpoint descriptor limit.");
        foreach (var descriptor in descriptors)
            if (string.IsNullOrWhiteSpace(descriptor.Id) || !MinimapTerrainCatalog.Finite(descriptor.WorldPosition))
                throw new ArgumentException("Invalid checkpoint descriptor.");
        for (int i = 0; i < descriptors.Length; i++)
            for (int j = 0; j < i; j++) if (descriptors[i].Id == descriptors[j].Id)
                throw new ArgumentException("Duplicate checkpoint descriptor.");
        _checkpoints = descriptors.ToArray(); _configured = true;
    }

    public void Present(ExplorationMapModel model, Vector2 actorWorld, Vector2 facing, double delta)
    {
        if (model == null || !MinimapTerrainCatalog.Finite(actorWorld) || !MinimapTerrainCatalog.Finite(facing) ||
            !double.IsFinite(delta) || Size.X <= 0 || Size.Y <= 0 || _surface == null) { ClearPresentation(); return; }
        bool changed = !ReferenceEquals(_model, model);
        if (changed)
        {
            _model = model;
            if (!ReferenceEquals(_catalog, model.Catalog)) { _catalog = model.Catalog; BuildBoundaryDirectory(); }
            _hasCenter = false;
        }
        _center = !_hasCenter ? actorWorld : _center.Lerp(actorWorld, (float)(1 - Math.Exp(-12 * Math.Max(0, delta))));
        if (!MinimapTerrainCatalog.Finite(_center)) { ClearPresentation(); return; }
        _actor = actorWorld; _hasCenter = true; _facing = facing.LengthSquared() > .001f ? facing.Normalized() : _facing;
        Vector2 worldSize = new(WorldViewWidth, WorldViewWidth * Size.Y / Size.X);
        if (worldSize.Y > CacheHeight * 8 - 32) { ClearPresentation(); return; }
        // Preserve ordinary smoothing, but a discontinuous recovery/mount jump must
        // not leave the actor unreadable outside the complete drawable marker area.
        Vector2 smoothedActor = Size * .5f + (_actor - _center) * Size / worldSize;
        if (!ActorFits(smoothedActor, _facing)) _center = actorWorld;
        ViewWorldRect = new Rect2(_center - worldSize * .5f, worldSize);
        string reason = changed ? "model" : _revision != model.RevisionCounter ? "dirty" :
            !CacheWorldRect.Grow(-16).Encloses(ViewWorldRect) ? "coverage" : "none";
        if (reason != "none") RebuildCache(reason);
        Vector2 samplingOrigin = ViewWorldRect.Position - CacheWorldRect.Position;
        if (samplingOrigin != _lastSampling || worldSize != _lastWorldSize || Size != _lastSize)
        {
            if (Size != _lastSize)
            {
                _maskMaterial.SetShaderParameter("outer_feather", EffectiveOuterFeather);
                _boundaryMaterial.SetShaderParameter("outer_feather", EffectiveOuterFeather);
            }
            _maskMaterial.SetShaderParameter("view_offset", samplingOrigin);
            _maskMaterial.SetShaderParameter("world_size", worldSize);
            _maskMaterial.SetShaderParameter("logical_size", Size);
            _boundaryMaterial.SetShaderParameter("view_offset", samplingOrigin);
            _boundaryMaterial.SetShaderParameter("world_size", worldSize);
            _boundaryMaterial.SetShaderParameter("logical_size", Size);
            _boundaryLayer.Position = -samplingOrigin * Size / worldSize;
            _boundaryLayer.Scale = Size / worldSize;
            _lastSampling = samplingOrigin; _lastWorldSize = worldSize; _lastSize = Size;
            QueueRedraw();
        }
        if (!Visible || reason != "none" || _actor != _lastActor || _facing != _lastFacing) QueueRedraw();
        _lastActor = _actor; _lastFacing = _facing; Visible = true;
    }

    public void ClearPresentation()
    {
        Visible = false; _hasCenter = false; DiscoveredCheckpointCount = 0;
        // Retain the cache and, critically, never clear/replace the model here.
    }

    private void BuildBoundaryDirectory()
    {
        _bucketColumns = (_catalog.Columns + 7) / 8; _bucketRows = (_catalog.Rows + 7) / 8;
        _heads = new int[_bucketColumns * _bucketRows]; Array.Fill(_heads, -1);
        _next = new int[_catalog.BoundaryCount];
        for (int id = 0; id < _next.Length; id++)
        {
            var edge = _catalog.Boundary(id); var solid = _catalog.Solid(edge.ShapeId);
            Vector2 point = solid.LocalToWorld * ((edge.FromLocal + edge.ToLocal) * .5f) - _catalog.WorldBounds.Position;
            int x = Math.Clamp((int)Math.Floor(point.X / 64), 0, _bucketColumns - 1);
            int y = Math.Clamp((int)Math.Floor(point.Y / 64), 0, _bucketRows - 1);
            int bucket = y * _bucketColumns + x; _next[id] = _heads[bucket]; _heads[bucket] = id;
        }
    }

    private void RebuildCache(string reason)
    {
        Vector2 lattice = _catalog.WorldBounds.Position;
        Vector2 origin = lattice + new Vector2(Mathf.Floor((_center.X - lattice.X) / 8) - CacheWidth / 2,
            Mathf.Floor((_center.Y - lattice.Y) / 8) - CacheHeight / 2) * 8;
        CacheWorldRect = new Rect2(origin, new Vector2(CacheWidth, CacheHeight) * 8);
        int col = (int)Math.Round((origin.X - lattice.X) / 8), row = (int)Math.Round((origin.Y - lattice.Y) / 8);
        for (int y = 0; y < CacheHeight; y++) for (int x = 0; x < CacheWidth; x++)
        {
            int gx = col + x, gy = row + y;
            _pixels[y * CacheWidth + x] = gx >= 0 && gy >= 0 && gx < _catalog.Columns && gy < _catalog.Rows &&
                _model.IsCellExplored(gy * _catalog.Columns + gx) ? (byte)255 : (byte)0;
        }
        _image.SetData(CacheWidth, CacheHeight, false, Image.Format.R8, _pixels);
        _texture.Update(_image); TextureUploadCount++; CacheRebuildCount++; LastCacheReason = reason;
        _revision = _model.RevisionCounter;
        _boundaryCount = LastBoundaryVisits = 0; BoundaryQueryOverflow = false;
        Rect2 local = CacheWorldRect.Grow(8); Vector2 start = local.Position - lattice, end = local.End - lattice;
        int x0 = Math.Clamp((int)Math.Floor(start.X / 64), 0, _bucketColumns - 1);
        int y0 = Math.Clamp((int)Math.Floor(start.Y / 64), 0, _bucketRows - 1);
        int x1 = Math.Clamp((int)Math.Floor(end.X / 64), 0, _bucketColumns - 1);
        int y1 = Math.Clamp((int)Math.Floor(end.Y / 64), 0, _bucketRows - 1);
        for (int y = y0; y <= y1; y++) for (int x = x0; x <= x1; x++)
            for (int id = _heads[y * _bucketColumns + x]; id != -1; id = _next[id])
            {
                if (LastBoundaryVisits == MaximumBoundaryVisits) { BoundaryQueryOverflow = true; goto Done; }
                LastBoundaryVisits++;
                if (!_model.IsBoundaryExplored(id)) continue;
                if (_boundaryCount == MaximumLocalBoundaries) { BoundaryQueryOverflow = true; goto Done; }
                _localBoundaries[_boundaryCount++] = id;
            }
        Done:
        // Conservative overflow: no partial dense bucket can imply an outline.
        if (BoundaryQueryOverflow) _boundaryCount = 0;
        _boundaryLayer.QueueRedraw();
    }

    internal void DrawBoundaries(MinimapBoundaryLayer layer)
    {
        var points = new Vector2[4]; var colors = new[] { Colors.White, Colors.White, Colors.White, Colors.White };
        var uvs = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) };
        for (int i = 0; i < _boundaryCount; i++)
        {
            var edge = _catalog.Boundary(_localBoundaries[i]); var solid = _catalog.Solid(edge.ShapeId);
            Vector2 a = solid.LocalToWorld * edge.FromLocal - CacheWorldRect.Position;
            Vector2 b = solid.LocalToWorld * edge.ToLocal - CacheWorldRect.Position;
            Vector2 tangent = (b - a).Normalized(); Vector2 normal = new(-tangent.Y, tangent.X);
            Vector2 expected = solid.LocalToWorld.BasisXform(edge.OutwardLocal);
            if (normal.Dot(expected) < 0) normal = -normal;
            // Stroke only the authorized outward/free side; never the wall interior.
            points[0] = a; points[1] = b; points[2] = b + normal * 8; points[3] = a + normal * 8;
            layer.DrawPrimitive(points, colors, uvs);
        }
    }

    public override void _Draw()
    {
        if (_model == null || !Visible) return;
        Vector2 factor = Size / ViewWorldRect.Size;
        DiscoveredCheckpointCount = 0;
        foreach (var checkpoint in _checkpoints)
        {
            Vector2 offset = checkpoint.WorldPosition - _catalog.WorldBounds.Position;
            int x = (int)Math.Floor(offset.X / 8), y = (int)Math.Floor(offset.Y / 8);
            if (x < 0 || y < 0 || x >= _catalog.Columns || y >= _catalog.Rows ||
                !_model.IsCellExplored(y * _catalog.Columns + x)) continue;
            DiscoveredCheckpointCount++;
            Vector2 p = (checkpoint.WorldPosition - ViewWorldRect.Position) * factor;
            if (FitsOpaqueEllipse(new Rect2(p - Vector2.One * 2.5f, Vector2.One * 5)))
                DrawCircle(p, 2.5f, new Color(.78f, .72f, .47f));
        }
        Vector2 actor = (_actor - ViewWorldRect.Position) * factor;
        if (!ActorFits(actor, _facing)) return;
        DrawCircle(actor, 5f, new Color(.025f, .035f, .035f));
        DrawLine(actor, actor + _facing * 9, new Color(.025f, .035f, .035f), 4f, false);
        DrawCircle(actor, 3.5f, new Color(.83f, .92f, .87f));
        DrawLine(actor, actor + _facing * 9, new Color(.83f, .92f, .87f), 2f, false);
    }

    private float EffectiveOuterFeather => Math.Min(OuterFeather, Math.Min(Size.X, Size.Y) * .2f);

    private bool FitsOpaqueEllipse(Rect2 footprint)
    {
        Vector2 halfSize = Size * .5f;
        float innerScale = 1f - EffectiveOuterFeather / Math.Min(halfSize.X, halfSize.Y);
        Vector2 radii = halfSize * innerScale;
        if (radii.X <= 0 || radii.Y <= 0) return false;
        // Local -> registered canvas -> viewport output. GetViewportTransform
        // already includes the viewport's final stretch; never multiply the
        // root's ContentScaleFactor again, and discard all translation here.
        Transform2D localToOutput = GetViewportTransform() * GetGlobalTransform();
        localToOutput.Origin = Vector2.Zero;
        float determinant = localToOutput.Determinant();
        if (!float.IsFinite(determinant) || determinant == 0) return false;
        Transform2D outputToLocal = localToOutput.AffineInverse();
        // The inverse basis maps a final output pixel's half-square into local
        // space, including nonuniform scale/rotation. .501 is numerical slack
        // around the physical half pixel, not a fixed logical-pixel padding.
        Vector2 halfPixel = new(.501f * (Math.Abs(outputToLocal.X.X) + Math.Abs(outputToLocal.Y.X)),
            .501f * (Math.Abs(outputToLocal.X.Y) + Math.Abs(outputToLocal.Y.Y)));
        if (!MinimapTerrainCatalog.Finite(halfPixel)) return false;
        Vector2 from = footprint.Position - halfSize - halfPixel, to = footprint.End - halfSize + halfPixel;
        // The worst of the four bounding-box vertices proves containment of
        // the complete circle/stroke in the convex opaque ellipse, not its centre.
        Vector2 farthest = new(Math.Max(Math.Abs(from.X), Math.Abs(to.X)) / radii.X,
            Math.Max(Math.Abs(from.Y), Math.Abs(to.Y)) / radii.Y);
        return farthest.LengthSquared() <= 1f;
    }

    private bool ActorFits(Vector2 actor, Vector2 facing)
    {
        Vector2 end = actor + facing * 9;
        Rect2 footprint = new Rect2(actor - Vector2.One * 5f, Vector2.One * 10)
            .Merge(new Rect2(end - Vector2.One * 2f, Vector2.One * 4));
        return FitsOpaqueEllipse(footprint);
    }
}

public partial class MinimapBoundaryLayer : Node2D
{
    internal PrologueMinimapView View;
    public override void _Draw() => View?.DrawBoundaries(this);
}
