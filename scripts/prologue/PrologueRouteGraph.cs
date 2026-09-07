using System;
using System.Collections.Generic;
using Godot;

namespace Knight.Prologue;

public sealed class PrologueRouteGraph
{
    private readonly Dictionary<PrologueRoomId, RoomDefinition> _rooms = new();
    private readonly Dictionary<(PrologueRoomId Room, StringName Exit), PrologueRoomLink> _links = new();

    public PrologueRouteGraph(IReadOnlyList<RoomDefinition> definitions)
    {
        if (definitions == null) throw new ArgumentNullException(nameof(definitions));
        foreach (RoomDefinition definition in definitions)
        {
            if (definition == null) throw new ArgumentException("Room definitions must not contain null.", nameof(definitions));
            IReadOnlyList<string> errors = definition.Validate();
            if (errors.Count > 0)
                throw new ArgumentException($"Invalid room definition {definition.RoomId}: {string.Join("; ", errors)}", nameof(definitions));
            if (!_rooms.TryAdd(definition.RoomId, definition))
                throw new ArgumentException($"Duplicate room definition: {definition.RoomId}", nameof(definitions));
        }

        foreach (RoomDefinition definition in definitions)
        foreach (PrologueRoomLinkResource authored in definition.OutgoingLinks)
        {
            PrologueRoomLink link = authored.ToLink();
            if (!_rooms.ContainsKey(link.SourceRoom) || !_rooms.ContainsKey(link.DestinationRoom))
                throw new ArgumentException(
                    $"Link {link.SourceRoom}/{link.ExitId} references a room outside the provided definitions.",
                    nameof(definitions));
            if (!_links.TryAdd((link.SourceRoom, link.ExitId), link))
                throw new ArgumentException($"Duplicate route key: {link.SourceRoom}/{link.ExitId}", nameof(definitions));
        }
    }

    public bool TryResolve(PrologueRoomId room, StringName exitId, out PrologueRoomLink link) =>
        _links.TryGetValue((room, exitId), out link);

    public IReadOnlyList<string> ValidateRequiredRooms(IReadOnlySet<PrologueRoomId> requiredRooms)
    {
        var errors = new List<string>();
        if (requiredRooms == null)
        {
            errors.Add("RequiredRooms must not be null");
            return errors;
        }
        foreach (PrologueRoomId room in requiredRooms)
        {
            if (!Enum.IsDefined(room)) errors.Add($"Required room value '{room}' is undefined");
            else if (!_rooms.ContainsKey(room)) errors.Add($"Required room '{room}' has no definition");
        }
        return errors;
    }
}
