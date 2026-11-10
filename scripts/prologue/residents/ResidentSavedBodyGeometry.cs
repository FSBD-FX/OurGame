using System;
using System.Collections.Generic;
using Godot;

namespace Knight.Prologue.Residents;

// Owner-scoped source acquisition. Never owns native disposal or body snapshots.
internal sealed class ResidentOriginalSceneSources : IDisposable
{
    private readonly HashSet<string> _allowed;
    private readonly Func<string, PackedScene> _load;
    private readonly Dictionary<string, PackedScene> _scenes = new(StringComparer.Ordinal);
    private bool _disposed;
#if REFCOUNTED_FATAL_IDENTITY
    private readonly long _traceCacheId = Knight.Tests.Diagnostics.Task14NativeResourceIdentity.NextSerial();
    private readonly Dictionary<string, ulong> _traceSceneIds = new(StringComparer.Ordinal);
    private string _traceOwner = "", _traceRole = "unclassified";
    internal void TraceOwner(string owner, string role) { _traceOwner = owner; _traceRole = role; }
    private void Trace(string kind, string path, ulong scene = 0) =>
        Knight.Tests.Diagnostics.ResidentProfileLifetimeTrace.Source(_traceCacheId, kind, scene, path, _traceOwner, _traceRole);
    private void TraceValid(string kind, string path, PackedScene scene) => _traceSceneIds[path] =
        Knight.Tests.Diagnostics.ResidentProfileLifetimeTrace.SourceValid(_traceCacheId, kind, scene, path, _traceOwner, _traceRole);
#endif
    internal ResidentOriginalSceneSources(IEnumerable<string> allowed, Func<string, PackedScene> load)
    { _allowed = new(allowed, StringComparer.Ordinal); _load = load; }
    internal PackedScene Get(string path)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(ResidentOriginalSceneSources));
        if (!_allowed.Contains(path)) throw new ArgumentException("Unknown original scene source", nameof(path));
        if (_scenes.TryGetValue(path, out var existing) && GodotObject.IsInstanceValid(existing))
        {
#if REFCOUNTED_FATAL_IDENTITY
            TraceValid("source-cache-hit", path, existing);
#endif
            return existing;
        }
        _scenes.Remove(path);
#if REFCOUNTED_FATAL_IDENTITY
        _traceSceneIds.Remove(path);
        Trace("source-load-enter", path);
#endif
        PackedScene loaded = _load(path);
        if (_disposed) throw new ObjectDisposedException(nameof(ResidentOriginalSceneSources));
        if (GodotObject.IsInstanceValid(loaded))
        {
            _scenes.Add(path, loaded);
#if REFCOUNTED_FATAL_IDENTITY
            TraceValid("source-valid-return", path, loaded);
#endif
        }
        return loaded;
    }
    public void Dispose()
    {
        _disposed = true; _scenes.Clear();
#if REFCOUNTED_FATAL_IDENTITY
        foreach (var entry in _traceSceneIds) Trace("cache-reference-clear", entry.Key, entry.Value);
        _traceSceneIds.Clear();
#endif
    }
}

// Saved-data reader, not a runtime collision or resource-lifetime certificate.
internal static class ResidentSavedBodyGeometry
{
    internal static bool TryRead(PackedScene scene, out Rect2 localBounds, out string reason)
    {
        localBounds = default;
        reason = "";
        var visiting = new HashSet<ulong>();
        int budget = 4096;
        return Read(scene, false, visiting, 0, ref budget, out localBounds, out reason);
    }

    private static bool Read(PackedScene scene, bool visualOnly, HashSet<ulong> visiting,
        int depth, ref int budget, out Rect2 bounds, out string reason)
    {
        bounds = default; reason = "";
        if (!GodotObject.IsInstanceValid(scene)) { reason = "missing_saved_scene"; return false; }
        if (depth > 32 || !visiting.Add(scene.GetInstanceId())) { reason = "nested_scene_cycle_or_depth"; return false; }
        try
        {
            // These are borrowed scene/state/shape objects, not private copies.
            // Never Dispose or retain them in a runtime cache.
            SceneState state = scene.GetState();
            if (state == null || state.GetNodeCount() == 0) { reason = "empty_saved_scene"; return false; }
            if (state.GetBaseSceneState() != null) { reason = "inherited_body_scene"; return false; }
            var owners = new Dictionary<string, bool>(StringComparer.Ordinal) { ["."] = !visualOnly };
            var instancePaths = new List<string>();
            int bodies = 0;
            for (int i = 0; i < state.GetNodeCount(); i++)
            {
                if (--budget < 0) { reason = "saved_scene_node_budget"; return false; }
                using NodePath nodePath = state.GetNodePath(i);
                using NodePath parentPath = state.GetNodePath(i, true);
                string path = i == 0 ? "." : RelativePath(nodePath), parent = i == 0 ? "" : RelativePath(parentPath);
                if (path == null || parent == null) { reason = "unsupported_node_path"; return false; }
                if (state.IsNodeInstancePlaceholder(i)) { reason = "scene_placeholder"; return false; }
                PackedScene instance = state.GetNodeInstance(i);
                using StringName savedType = state.GetNodeType(i);
                string type = savedType.ToString();
                if (i == 0)
                {
                    if (instance != null || (!visualOnly && type != "CharacterBody2D"))
                    { reason = "unsupported_body_root"; return false; }
                    if (!visualOnly && !FalseProperty(state, i, "top_level"))
                    { reason = "top_level_body_root"; return false; }
                }
                else
                {
                    foreach (string opaque in instancePaths)
                        if (path.StartsWith(opaque + "/", StringComparison.Ordinal))
                        { reason = "nested_instance_override"; return false; }
                    if (!owners.TryGetValue(parent, out _)) { reason = "unsupported_node_parent"; return false; }
                }
                if (instance != null)
                {
                    if (i == 0 || !Read(instance, true, visiting, depth + 1, ref budget, out _, out reason)) return false;
                    instancePaths.Add(path);
                    // Empty saved types on genuine instances are resolved only
                    // from their separately inspected noninherited root state.
                    if (type.Length == 0)
                    {
                        using StringName nestedType = instance.GetState().GetNodeType(0);
                        type = nestedType.ToString();
                    }
                }
                if (type.Length == 0 || !ClassDB.ClassExists(type)) { reason = "unknown_saved_node_type"; return false; }
                bool collisionObject = ClassDB.IsParentClass(type, "CollisionObject2D");
                bool shape = ClassDB.IsParentClass(type, "CollisionShape2D");
                bool polygon = ClassDB.IsParentClass(type, "CollisionPolygon2D");
                if (visualOnly && (collisionObject || shape || polygon)) { reason = "nested_visual_physics"; return false; }
                if (!visualOnly && i != 0 && collisionObject && !ClassDB.IsParentClass(type, "Area2D"))
                { reason = "additional_physics_body"; return false; }
                bool ownsActorBody = i == 0 ? !visualOnly : owners[parent] && !collisionObject;
                owners[path] = ownsActorBody;
                if (!visualOnly && ownsActorBody && polygon) { reason = "body_polygon"; return false; }
                if (!visualOnly && ownsActorBody && shape)
                {
                    if (++bodies != 1) { reason = "multiple_body_shapes"; return false; }
                    if (parent != "." || path != "BodyCollider") { reason = "unsupported_body_path"; return false; }
                    if (!ReadCollider(state, i, out bounds, out reason)) return false;
                }
            }
            if (!visualOnly && bodies != 1) { reason = "missing_body_collider"; return false; }
            return true;
        }
        finally { visiting.Remove(scene.GetInstanceId()); }
    }

    private static bool ReadCollider(SceneState state, int index, out Rect2 bounds, out string reason)
    {
        bounds = default; reason = "";
        if (!FalseProperty(state, index, "disabled") || !FalseProperty(state, index, "one_way_collision") ||
            !FalseProperty(state, index, "top_level")) { reason = "disabled_one_way_or_top_level_body"; return false; }
        using var script = Property(state, index, "script");
        if (script.VariantType != Variant.Type.Nil &&
            (script.VariantType != Variant.Type.Object || script.AsGodotObject() != null))
        { reason = "scripted_body_collider"; return false; }
        using var rotation = Property(state, index, "rotation");
        using var skew = Property(state, index, "skew");
        using var scale = Property(state, index, "scale");
        using var transform = Property(state, index, "transform");
        if (!Zero(rotation) || !Zero(skew) ||
            scale.VariantType != Variant.Type.Nil && (scale.VariantType != Variant.Type.Vector2 || scale.AsVector2() != Vector2.One) ||
            transform.VariantType != Variant.Type.Nil && (transform.VariantType != Variant.Type.Transform2D || transform.AsTransform2D() != Transform2D.Identity))
        { reason = "unsupported_body_transform"; return false; }
        using var position = Property(state, index, "position");
        if (position.VariantType != Variant.Type.Nil && position.VariantType != Variant.Type.Vector2)
        { reason = "invalid_body_translation"; return false; }
        Vector2 offset = position.VariantType == Variant.Type.Nil ? Vector2.Zero : position.AsVector2();
        using var value = Property(state, index, "shape");
        if (value.VariantType != Variant.Type.Object || value.AsGodotObject() is not Shape2D body ||
            body is not (CapsuleShape2D or RectangleShape2D or CircleShape2D))
        { reason = "unsupported_body_shape"; return false; }
        using var bodyScript = body.GetScript();
        if (bodyScript.VariantType != Variant.Type.Nil &&
            (bodyScript.VariantType != Variant.Type.Object || bodyScript.AsGodotObject() != null))
        { reason = "scripted_body_shape"; return false; }
        Rect2 rect = body.GetRect();
        bounds = new(rect.Position + offset, rect.Size);
        if (!Knight.Prologue.Seamless.PrologueSectorDefinition.Finite(offset) ||
            !Knight.Prologue.Seamless.PrologueSectorDefinition.FinitePositive(bounds))
        { bounds = default; reason = "invalid_body_bounds"; return false; }
        return true;
    }

    private static Variant Property(SceneState state, int node, string property)
    {
        for (int i = 0; i < state.GetNodePropertyCount(node); i++)
        {
            using StringName name = state.GetNodePropertyName(node, i);
            if (name.ToString() == property) return state.GetNodePropertyValue(node, i);
        }
        return default;
    }
    private static bool FalseProperty(SceneState state, int node, string name)
    {
        using var value = Property(state, node, name);
        return value.VariantType == Variant.Type.Nil || value.VariantType == Variant.Type.Bool && !value.AsBool();
    }
    private static bool Zero(Variant value) => value.VariantType == Variant.Type.Nil ||
        value.VariantType is Variant.Type.Float or Variant.Type.Int && value.AsDouble() == 0d;

    private static string RelativePath(NodePath value)
    {
        if (value.IsAbsolute()) return null;
        string path = value.ToString();
        if (path.StartsWith("./", StringComparison.Ordinal)) path = path[2..];
        if (path == ".") return path;
        foreach (string part in path.Split('/'))
            if (part.Length == 0 || part is "." or "..") return null;
        return path;
    }
}
