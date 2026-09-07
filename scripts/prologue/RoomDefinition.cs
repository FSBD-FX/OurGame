using System;
using System.Collections.Generic;
using Godot;

namespace Knight.Prologue;

[GlobalClass]
public partial class RoomDefinition : Resource
{
    [Export] public PrologueRoomId RoomId { get; set; }
    [Export] public PackedScene Scene { get; set; }
    [Export] public StringName StartEntrance { get; set; } = "start";
    [Export] public Rect2 CameraBounds { get; set; } = new(0, 0, 1280, 720);
    [Export] public bool HasCheckpoint { get; set; }
    [Export] public StringName CheckpointId { get; set; }
    [Export] public Godot.Collections.Array<Resource> EncounterResources { get; set; } = new();
    [Export] public Godot.Collections.Array<PrologueRoomLinkResource> OutgoingLinks { get; set; } = new();

    public IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();
        if (!Enum.IsDefined(RoomId)) errors.Add($"{nameof(RoomId)} is undefined");
        if (Scene == null) errors.Add($"{nameof(Scene)} is required");
        if (Empty(StartEntrance)) errors.Add($"{nameof(StartEntrance)} is required");
        if (!FinitePositive(CameraBounds.Size.X) || !FinitePositive(CameraBounds.Size.Y))
            errors.Add($"{nameof(CameraBounds)} must have positive finite width and height");
        if (HasCheckpoint && Empty(CheckpointId))
            errors.Add($"{nameof(CheckpointId)} is required when {nameof(HasCheckpoint)} is true");

        if (EncounterResources == null) errors.Add($"{nameof(EncounterResources)} must not be null");
        else
        {
            var encounterIds = new HashSet<StringName>();
            for (int i = 0; i < EncounterResources.Count; i++)
            {
                if (EncounterResources[i] is not EncounterDefinition encounter)
                {
                    errors.Add($"{nameof(EncounterResources)}[{i}] must be an {nameof(EncounterDefinition)}");
                    continue;
                }
                foreach (string error in encounter.Validate())
                    errors.Add($"encounter[{i}] {error}");
                if (!Empty(encounter.EncounterId) && !encounterIds.Add(encounter.EncounterId))
                    errors.Add($"duplicate encounter ID '{encounter.EncounterId}'");
            }
        }

        var exits = new HashSet<StringName>();
        if (OutgoingLinks == null)
        {
            errors.Add($"{nameof(OutgoingLinks)} must not be null");
            return errors;
        }
        for (int i = 0; i < OutgoingLinks.Count; i++)
        {
            PrologueRoomLinkResource authored = OutgoingLinks[i];
            if (authored == null)
            {
                errors.Add($"{nameof(OutgoingLinks)}[{i}] must not be null");
                continue;
            }
            if (authored.SourceRoom != RoomId)
                errors.Add($"{nameof(PrologueRoomLink.SourceRoom)} for exit '{authored.ExitId}' must match {RoomId}");
            if (Empty(authored.ExitId))
                errors.Add($"{nameof(PrologueRoomLink.ExitId)} is required at index {i}");
            else if (!exits.Add(authored.ExitId))
                errors.Add($"duplicate outgoing exit '{authored.ExitId}'");
            if (!Enum.IsDefined(authored.DestinationRoom))
                errors.Add($"{nameof(PrologueRoomLink.DestinationRoom)} is undefined for exit '{authored.ExitId}'");
            if (Empty(authored.EntranceId))
                errors.Add($"{nameof(PrologueRoomLink.EntranceId)} is required for exit '{authored.ExitId}'");
        }
        return errors;
    }

    private static bool FinitePositive(float value) => float.IsFinite(value) && value > 0f;
    private static bool Empty(StringName value) => value == null || value.IsEmpty;
}
