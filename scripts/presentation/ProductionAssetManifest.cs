using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using Godot;
using Knight.Prologue.Seamless;

namespace Knight.Presentation;

public sealed record ProductionAssetEntry(
    string Id,
    string Category,
    string RuntimePath,
    string Source,
    string License,
    bool Final);

public sealed record ProductionReplacementSlot(string Id, string Scope, string StablePath);

public sealed record ProductionAssetManifest(
    int Version,
    IReadOnlyList<ProductionAssetEntry> Assets,
    IReadOnlyList<ProductionReplacementSlot> ReplacementSlots)
{
    public IReadOnlyList<string> RequireRuntimeFiles(string projectRoot)
    {
        if (string.IsNullOrWhiteSpace(projectRoot))
            throw new ArgumentException("Project root is required.", nameof(projectRoot));

        string root = Path.GetFullPath(projectRoot);
        var missing = new List<string>();
        foreach (ProductionAssetEntry asset in Assets)
        {
            if (!asset.Final) continue;
            string relative = asset.RuntimePath["res://".Length..].Replace('/', Path.DirectorySeparatorChar);
            string candidate = Path.GetFullPath(Path.Combine(root, relative));
            if (!File.Exists(candidate)) missing.Add(asset.RuntimePath);
        }

        return new ReadOnlyCollection<string>(missing);
    }

    public IReadOnlyList<string> AuditReplacementBoundaries(Node level,
        IEnumerable<Node> sectorRoots)
    {
        var errors = new List<string>();
        List<Node> sectors = sectorRoots?.Where(sector => sector != null).ToList() ?? new();
        PrologueSectorId[] expectedSectorIds = Enum.GetValues<PrologueSectorId>();
        if (sectors.Count != expectedSectorIds.Length)
            errors.Add($"replacement sector roots must contain exactly five unique sectors; actual={sectors.Count}");
        var sectorIds = new HashSet<PrologueSectorId>();
        foreach (Node sectorRoot in sectors)
        {
            if (sectorRoot is not PrologueSector sector)
            {
                errors.Add($"replacement sector root {sectorRoot.Name} has the wrong sector identity type");
                continue;
            }
            if (!Enum.IsDefined(sector.SectorId))
                errors.Add($"replacement sector root {sector.Name} has an undefined sector id");
            else if (!sectorIds.Add(sector.SectorId))
                errors.Add($"duplicate replacement sector id {sector.SectorId}");
            if (!string.Equals(sector.Name.ToString(), sector.SectorId.ToString(),
                    StringComparison.Ordinal))
                errors.Add($"replacement sector identity {sector.Name} does not match id {sector.SectorId}");
        }
        foreach (PrologueSectorId expected in expectedSectorIds)
            if (!sectorIds.Contains(expected)) errors.Add($"missing replacement sector id {expected}");

        Node player = level?.GetNodeOrNull("PlayerRoot/Player");
        foreach (ProductionReplacementSlot slot in ReplacementSlots)
        {
            bool found = slot.Scope switch
            {
                "hud" => level?.GetNodeOrNull(slot.StablePath) != null,
                "player" => player?.GetNodeOrNull(slot.StablePath["Player/".Length..]) != null,
                "sector" => sectors.Count == expectedSectorIds.Length && sectors.All(
                    sector => sector?.GetNodeOrNull(slot.StablePath) != null),
                _ => false
            };
            if (!found) errors.Add($"missing replacement slot {slot.Id} at {slot.StablePath}");
        }

        if (level != null) AuditVisualRoots(level, errors);
        if (sectors.Count > 0)
            foreach (Node sector in sectors)
                if (sector != null) AuditVisualRoots(sector, errors);
        return new ReadOnlyCollection<string>(errors);
    }

    private static void AuditVisualRoots(Node root, List<string> errors)
    {
        foreach (Node node in Descendants(root))
        {
            if (node.Name != "VisualRoot") continue;
            foreach (Node visual in Descendants(node))
            {
                if (visual is CollisionObject2D or CollisionShape2D or CollisionPolygon2D)
                    errors.Add($"gameplay collision is inside VisualRoot at {visual.Name}");
                if (OwnsAuthoritativeState(visual))
                    errors.Add($"placeholder owns combat state at {visual.Name}");
            }
        }
    }

    private static IEnumerable<Node> Descendants(Node root)
    {
        foreach (Node child in root.GetChildren())
        {
            yield return child;
            foreach (Node descendant in Descendants(child)) yield return descendant;
        }
    }

    private static bool OwnsAuthoritativeState(Node node)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public |
                                   BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
        string[] forbidden =
        [
            "CurrentHealth", "Damage", "Checkpoint", "TakeDamage", "ApplyDamage",
            "TryResolve", "ResetFull", "RequestRespawn"
        ];
        Type type = node.GetType();
        return type.GetProperties(flags).Any(property => forbidden.Contains(property.Name,
                   StringComparer.Ordinal)) ||
               type.GetMethods(flags).Any(method => forbidden.Contains(method.Name,
                   StringComparer.Ordinal));
    }
}
