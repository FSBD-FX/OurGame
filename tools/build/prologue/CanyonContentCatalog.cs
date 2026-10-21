using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Knight.Prologue.Seamless;

namespace Knight.Build.Prologue;

public static class CanyonContentCatalog
{
    private static readonly IReadOnlyList<CanyonMicroSceneDefinition> Entries = Array.AsReadOnly(
        CanyonWorldLayout.Nodes.Select(node => new CanyonMicroSceneDefinition(
            node.Id.ToString(), node.SectorId,
            new Rect2(node.WorldBounds.Position - CanyonWorldLayout.SectorBounds[node.SectorId].Position,
                node.WorldBounds.Size), node.Optional, node.ChallengeKind, node.Id)).ToArray());

    public static IReadOnlyList<CanyonMicroSceneDefinition> All => Entries;

    public static IReadOnlyList<CanyonMicroSceneDefinition> ForSector(PrologueSectorId sectorId) =>
        Array.AsReadOnly(Entries.Where(item => item.SectorId == sectorId).ToArray());

    public static IReadOnlyList<CanyonEncounterDefinition> EncountersForSector(
        PrologueSectorId sectorId) => Array.AsReadOnly(CanyonEncounterCatalog.Normal
        .Where(encounter => CanyonWorldLayout.Nodes.Single(node => node.Id == encounter.RouteNode)
            .SectorId == sectorId).ToArray());
}
