using System;
using System.Collections.Generic;
using Godot;

namespace Knight.Prologue;

[GlobalClass]
public partial class EncounterDefinition : Resource
{
    [Export] public StringName EncounterId { get; set; }
    [Export] public Godot.Collections.Array<EncounterSpawn> Spawns { get; set; } = new();

    public IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();
        if (Empty(EncounterId)) errors.Add($"{nameof(EncounterId)} is required");
        if (Spawns == null)
        {
            errors.Add($"{nameof(Spawns)} must not be null");
            return errors;
        }
        if (Spawns.Count == 0) errors.Add($"{nameof(Spawns)} requires at least one spawn");

        var ids = new HashSet<StringName>();
        for (int i = 0; i < Spawns.Count; i++)
        {
            EncounterSpawn spawn = Spawns[i];
            if (spawn == null)
            {
                errors.Add($"{nameof(Spawns)}[{i}] must not be null");
                continue;
            }
            if (Empty(spawn.SpawnId)) errors.Add($"{nameof(EncounterSpawn.SpawnId)} is required at index {i}");
            else if (!ids.Add(spawn.SpawnId)) errors.Add($"duplicate spawn ID '{spawn.SpawnId}'");
            if (spawn.Scene == null) errors.Add($"{nameof(EncounterSpawn.Scene)} is required for '{spawn.SpawnId}'");
            if (spawn.MarkerPath == null || spawn.MarkerPath.IsEmpty)
                errors.Add($"{nameof(EncounterSpawn.MarkerPath)} is required for '{spawn.SpawnId}'");
        }
        return errors;
    }

    private static bool Empty(StringName value) => value == null || value.IsEmpty;
}
