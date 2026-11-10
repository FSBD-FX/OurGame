using System;
using System.Collections.Generic;
using System.Linq;
using Knight.Build.Prologue;

namespace Knight.Prologue.Residents;

public enum ResidentContentMode { FinalOnly, DevelopmentV001 }

// Explicit content selection is not a final-art or placement certificate.
public static class ResidentSceneRegistry
{
    public const string DevelopmentRoot = "res://scenes/enemies/residents/v001/";
    private static readonly IReadOnlyList<ResidentSubtypeDefinition> Development = Array.AsReadOnly(
        CanyonResidentCatalog.Subtypes.Select(type => type with
        {
            SceneState = ResidentSceneState.ExplicitPlaceholder,
            ScenePath = DevelopmentRoot + type.Id + ".tscn"
        }).ToArray());
    public static IReadOnlyList<ResidentSubtypeDefinition> Select(ResidentContentMode mode)
        => mode switch
        {
            ResidentContentMode.FinalOnly => CanyonResidentCatalog.Subtypes,
            ResidentContentMode.DevelopmentV001 => Development,
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unknown resident content mode.")
        };
}
