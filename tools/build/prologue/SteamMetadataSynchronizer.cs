using System;
using System.Linq;
using Godot;
using Knight.Presentation;
using Knight.Prologue.Seamless;

namespace Knight.Build.Prologue;

// Narrow authoring operation; it must never replace the canonical physical sector with a donor.
public static class SteamMetadataSynchronizer
{
    // Archive has no readability wash. Its detached producer changes only these
    // two existing nodes' three scalar fields, never collision or actor content.
    public static void ApplyArchive(PrologueSector target, PrologueSector donor)
    {
        if(target==null||donor==null||target==donor||target.SectorId!=PrologueSectorId.CanyonDescent||donor.SectorId!=PrologueSectorId.CanyonDescent||
            target.GetParent()!=null||donor.GetParent()!=null||target.IsInsideTree()||donor.IsInsideTree()||target.IsQueuedForDeletion()||donor.IsQueuedForDeletion()||target.Bounds!=donor.Bounds)
            throw new ArgumentException("Two distinct detached Descent authoring roots with matching bounds are required.");
        const string markerPath="Gameplay/MicroScenes/ArchiveSideCave",compositionPath="VisualRoot/Midground/ChallengeCompositions/ArchiveSideCave";
        if(target.GetNodeOrNull(markerPath) is not Marker2D marker||donor.GetNodeOrNull(markerPath) is not Marker2D sourceMarker||
            target.GetNodeOrNull(compositionPath) is not CanyonBackdropVisual composition||donor.GetNodeOrNull(compositionPath) is not CanyonBackdropVisual sourceComposition||
            !marker.HasMeta("local_bounds")||!sourceMarker.HasMeta("local_bounds")||sourceMarker.GetMeta("local_bounds").VariantType!=Variant.Type.Rect2)
            throw new ArgumentException("Archive authoring nodes/metadata are missing or have a foreign class.");
        Rect2 bounds=sourceMarker.GetMeta("local_bounds").AsRect2();
        if(!float.IsFinite(bounds.Position.X)||!float.IsFinite(bounds.Position.Y)||!float.IsFinite(bounds.Size.X)||!float.IsFinite(bounds.Size.Y)||bounds.Size.X<=8||bounds.Size.Y<=8||
            !donor.Bounds.Encloses(bounds)||sourceMarker.Position!=bounds.GetCenter()||sourceComposition.CompositionBounds!=bounds||sourceComposition.Position!=Vector2.Zero)
            throw new ArgumentException("Archive donor bounds/centers/composition are inconsistent.");
        marker.SetMeta("local_bounds",bounds);
        marker.Position=sourceMarker.Position;
        composition.CompositionBounds=bounds;
    }

    public static void Apply(PrologueSector target, PrologueSector donor)
    {
        if (target==null || donor==null || target==donor || target.SectorId!=PrologueSectorId.CanyonBasin || donor.SectorId!=PrologueSectorId.CanyonBasin ||
            target.GetParent()!=null || donor.GetParent()!=null || target.IsInsideTree() || donor.IsInsideTree() || target.IsQueuedForDeletion() || donor.IsQueuedForDeletion() || target.Bounds!=donor.Bounds)
            throw new ArgumentException("Two distinct detached Basin authoring roots with matching bounds are required.");
        const string markerPath="Gameplay/MicroScenes/SteamValves", washPath="VisualRoot/GameplayReadable/MicroSceneReadability/SteamValves",
            compositionPath="VisualRoot/Midground/ChallengeCompositions/SteamValves";
        // Resolve and validate every destination and source before the first mutation.
        if (target.GetNodeOrNull(markerPath) is not Marker2D marker || donor.GetNodeOrNull(markerPath) is not Marker2D sourceMarker ||
            target.GetNodeOrNull(washPath) is not Polygon2D wash || donor.GetNodeOrNull(washPath) is not Polygon2D sourceWash ||
            target.GetNodeOrNull(compositionPath) is not CanyonBackdropVisual composition || donor.GetNodeOrNull(compositionPath) is not CanyonBackdropVisual sourceComposition ||
            !marker.HasMeta("local_bounds") || !sourceMarker.HasMeta("local_bounds") || sourceMarker.GetMeta("local_bounds").VariantType!=Variant.Type.Rect2)
            throw new ArgumentException("Steam authoring nodes/metadata are missing or have a foreign class.");
        Rect2 bounds=sourceMarker.GetMeta("local_bounds").AsRect2();
        Vector2[] points=sourceWash.Polygon;
        if (!float.IsFinite(bounds.Position.X) || !float.IsFinite(bounds.Position.Y) || !float.IsFinite(bounds.Size.X) || !float.IsFinite(bounds.Size.Y) ||
            bounds.Size.X<=8 || bounds.Size.Y<=8 || !donor.Bounds.Encloses(bounds) || sourceMarker.Position!=bounds.GetCenter() || sourceWash.Position!=bounds.GetCenter() ||
            sourceComposition.CompositionBounds!=bounds || sourceComposition.Position!=Vector2.Zero || points.Length!=4 || points.Any(p=>!float.IsFinite(p.X)||!float.IsFinite(p.Y)))
            throw new ArgumentException("Steam donor bounds/centers/readability are inconsistent.");
        marker.SetMeta("local_bounds",bounds);
        marker.Position=sourceMarker.Position;
        wash.Position=sourceWash.Position;
        wash.Polygon=(Vector2[])points.Clone();
        composition.CompositionBounds=sourceComposition.CompositionBounds;
    }
}
