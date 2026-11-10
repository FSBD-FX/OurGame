using System;
using Godot;
using Knight.Abilities;
using Knight.Presentation;
using Knight.Prologue.Seamless;

namespace Knight.Build.Prologue;

public static partial class CanyonContentBuilder
{
    // Explicit local coordinates for new authoring leaves. Existing Solid/world-offset paths stay unchanged.
    internal static StaticBody2D AuthoredLocalSolid(PrologueSector root,string name,Rect2 local,CanyonRockSkinRole role)
    {
        ArgumentNullException.ThrowIfNull(root);
        if(!PrologueSectorDefinition.FinitePositive(local) || !root.Bounds.Encloses(local) ||
            string.IsNullOrWhiteSpace(name) || name.Contains('/') || root.GetNode("Gameplay/Collision").HasNode(name))
            throw new ArgumentException("Authored solid requires unique name and finite contained local bounds.");
        StaticBody2D body=Platform(root,name,local.GetCenter(),local.Size,GrappleSurfaceMaterial.GripStone,rockRole:role);
        body.SetMeta(MinimapTerrainCatalogBuilder.StructuralMarker,MinimapTerrainCatalogBuilder.Provenance);
        return body;
    }
}
