using System;
using Godot;

namespace Knight.Presentation.Fog;

/// <summary>World-aligned, display-only fog raster consumer.</summary>
public partial class PrologueFogCanvas : ColorRect
{
    private const int MaxFields = 64;
    // Candidate accessibility tuning. Scale only the final display alpha;
    // never enlarge clear/beam radii or relax native occlusion.
    private const float ReducedOpacityScale = .5f;
    private readonly float[] _geometry = new float[HeadlampOcclusionSampler.MaximumShapes * 12];
    private readonly byte[] _geometryBytes = new byte[HeadlampOcclusionSampler.MaximumShapes * 48];
    private Image _geometryImage;
    private ImageTexture _geometryTexture;
    private ImageTexture _fieldTexture;
    private ShaderMaterial _fogMaterial;
    public bool FrameValid { get; private set; }

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        Color = Colors.White;
        _fogMaterial = new ShaderMaterial
        {
            Shader = GD.Load<Shader>("res://assets/shaders/prologue-fog.gdshader")
                ?? throw new InvalidOperationException("Headlamp fog shader is missing.")
        };
        Material = _fogMaterial;
        _geometryImage = Image.CreateFromData(3, HeadlampOcclusionSampler.MaximumShapes,
            false, Image.Format.Rgbaf, _geometryBytes);
        _geometryTexture = ImageTexture.CreateFromImage(_geometryImage);
        _fogMaterial.SetShaderParameter("occluders", _geometryTexture);
        UploadFields();
    }

    public void Present(Vector2 actorWorldPosition, Vector2 lampWorldPosition,
        Vector2 lampDirection, Transform2D viewportToWorld, HeadlampOcclusionFrame occlusion,
        bool reducedFog = false)
    {
        if (_fogMaterial == null) return;
        FrameValid = occlusion != null && occlusion.IsComplete &&
            occlusion.Shapes.Count <= HeadlampOcclusionSampler.MaximumShapes &&
            PrologueFogModel.Fields.Count <= MaxFields &&
            Finite(actorWorldPosition) && Finite(lampWorldPosition) && Finite(lampDirection) &&
            Finite(viewportToWorld.X) && Finite(viewportToWorld.Y) && Finite(viewportToWorld.Origin) &&
            float.IsFinite(viewportToWorld.Determinant()) && viewportToWorld.Determinant() != 0f &&
            ProjectionFitsViewport(viewportToWorld);

        int count = 0;
        if (FrameValid)
        {
            // All affected endpoints and both source-to-endpoint segments lie
            // inside this union AABB. Excluding a rectangle outside it cannot
            // grant light past a blocker; no angular sparse contour is used.
            float range = PrologueFogProfile.Exploration.OuterRadius;
            foreach (PrologueFogField field in PrologueFogModel.Fields)
                range = Mathf.Max(range, Mathf.Max(field.Profile.OuterRadius, field.Profile.BeamRange));
            range = Mathf.Max(range, PrologueFogProfile.Exploration.BeamRange);
            Vector2 min = actorWorldPosition.Min(lampWorldPosition) - Vector2.One * range;
            Vector2 max = actorWorldPosition.Max(lampWorldPosition) + Vector2.One * range;
            if (!Finite(min) || !Finite(max)) FrameValid = false;
            else foreach (HeadlampOccluder shape in occlusion.Shapes)
            {
                Transform2D forward = shape.WorldToLocal.AffineInverse();
                Rect2 rect = shape.LocalBounds;
                Vector2 a = forward * rect.Position;
                Vector2 b = forward * new Vector2(rect.End.X, rect.Position.Y);
                Vector2 c = forward * rect.End;
                Vector2 d = forward * new Vector2(rect.Position.X, rect.End.Y);
                if (!Finite(a) || !Finite(b) || !Finite(c) || !Finite(d))
                { FrameValid = false; break; }
                Vector2 shapeMin = a.Min(b).Min(c).Min(d), shapeMax = a.Max(b).Max(c).Max(d);
                if (shapeMax.X < min.X || shapeMin.X > max.X ||
                    shapeMax.Y < min.Y || shapeMin.Y > max.Y) continue;
                Transform2D inverse = shape.WorldToLocal;
                int offset = count++ * 12;
                _geometry[offset] = inverse.X.X; _geometry[offset + 1] = inverse.Y.X;
                _geometry[offset + 2] = inverse.Origin.X; _geometry[offset + 3] = rect.Position.X;
                _geometry[offset + 4] = inverse.X.Y; _geometry[offset + 5] = inverse.Y.Y;
                _geometry[offset + 6] = inverse.Origin.Y; _geometry[offset + 7] = rect.Position.Y;
                _geometry[offset + 8] = rect.End.X; _geometry[offset + 9] = rect.End.Y;
            }
        }
        Buffer.BlockCopy(_geometry, 0, _geometryBytes, 0, _geometryBytes.Length);
        _geometryImage.SetData(3, HeadlampOcclusionSampler.MaximumShapes, false, Image.Format.Rgbaf, _geometryBytes);
        _geometryTexture.Update(_geometryImage);
        _fogMaterial.SetShaderParameter("occluder_count", count);
        _fogMaterial.SetShaderParameter("frame_valid", FrameValid);
        _fogMaterial.SetShaderParameter("opacity_scale", reducedFog ? ReducedOpacityScale : 1f);
        _fogMaterial.SetShaderParameter("actor_position", FrameValid ? actorWorldPosition : Vector2.Zero);
        _fogMaterial.SetShaderParameter("lamp_position", FrameValid ? lampWorldPosition : Vector2.Zero);
        // Normalize using doubles before the GPU: extreme finite keyboard/source
        // direction cannot overflow a float length and turn into NaN lighting.
        double length = FrameValid ? Math.Sqrt((double)lampDirection.X * lampDirection.X +
            (double)lampDirection.Y * lampDirection.Y) : 0d;
        _fogMaterial.SetShaderParameter("lamp_direction", length > 0d
            ? new Vector2((float)(lampDirection.X / length), (float)(lampDirection.Y / length)) : Vector2.Zero);
        Transform2D matrix = FrameValid ? viewportToWorld : Transform2D.Identity;
        _fogMaterial.SetShaderParameter("screen_to_world_x", new Vector3(matrix.X.X, matrix.Y.X, matrix.Origin.X));
        _fogMaterial.SetShaderParameter("screen_to_world_y", new Vector3(matrix.X.Y, matrix.Y.Y, matrix.Origin.Y));
    }

    private void UploadFields()
    {
        int count = PrologueFogModel.Fields.Count;
        var values = new float[Math.Max(1, count) * 12];
        for (int index = 0; index < count; index++)
        {
            PrologueFogField field = PrologueFogModel.Fields[index];
            int offset = index * 12;
            values[offset] = field.Bounds.Position.X; values[offset + 1] = field.Bounds.Position.Y;
            values[offset + 2] = field.Bounds.End.X; values[offset + 3] = field.Bounds.End.Y;
            values[offset + 4] = field.Profile.ClearRadius; values[offset + 5] = field.Profile.OuterRadius;
            values[offset + 6] = field.Profile.MaxOpacity; values[offset + 7] = field.Profile.BeamRange;
            values[offset + 8] = field.Profile.BeamHalfAngleDegrees; values[offset + 9] = field.BlendWidth;
        }
        var bytes = new byte[values.Length * sizeof(float)];
        Buffer.BlockCopy(values, 0, bytes, 0, bytes.Length);
        using Image image = Image.CreateFromData(3, Math.Max(1, count), false, Image.Format.Rgbaf, bytes);
        _fieldTexture = ImageTexture.CreateFromImage(image);
        _fogMaterial.SetShaderParameter("fields", _fieldTexture);
        _fogMaterial.SetShaderParameter("field_count", Math.Min(count, MaxFields));
        PrologueFogProfile profile = PrologueFogProfile.Exploration;
        _fogMaterial.SetShaderParameter("base_profile", new Vector4(profile.ClearRadius,
            profile.OuterRadius, profile.MaxOpacity, profile.BeamRange));
        _fogMaterial.SetShaderParameter("base_half_angle", profile.BeamHalfAngleDegrees);
    }

    public override void _ExitTree()
    {
        FrameValid = false;
        Material = null;
        _fogMaterial?.Dispose(); _fogMaterial = null;
        _geometryTexture?.Dispose(); _geometryTexture = null;
        _geometryImage?.Dispose(); _geometryImage = null;
        _fieldTexture?.Dispose(); _fieldTexture = null;
        // The same node may be detached/re-added by a viewport or level host.
        // Godot otherwise calls _Ready only once, leaving a white rect without
        // its disposed material. Rebuild only the resources this canvas owns.
        RequestReady();
    }

    private bool ProjectionFitsViewport(Transform2D matrix)
    {
        // FRAGCOORD is in physical pixels, not the logical Control size. Finite
        // columns and determinant alone still allow a GPU dot product to overflow.
        // Bound every intermediate, even when very large terms would cancel.
        Vector2 size = GetViewport().GetTexture().GetSize();
        if (!Finite(size) || size.X <= 0f || size.Y <= 0f) return false;
        double limit = (double)float.MaxValue / 4d;
        double xBound = Math.Abs((double)matrix.Origin.X) +
            Math.Abs((double)matrix.X.X) * size.X + Math.Abs((double)matrix.Y.X) * size.Y;
        double yBound = Math.Abs((double)matrix.Origin.Y) +
            Math.Abs((double)matrix.X.Y) * size.X + Math.Abs((double)matrix.Y.Y) * size.Y;
        return xBound <= limit && yBound <= limit;
    }

    private static bool Finite(Vector2 value) => float.IsFinite(value.X) && float.IsFinite(value.Y);
}
