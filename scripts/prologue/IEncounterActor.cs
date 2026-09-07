using System;

namespace Knight.Prologue;

public interface IEncounterActor : ICheckpointResettable
{
    event Action Defeated;
}
