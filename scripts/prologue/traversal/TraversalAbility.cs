using System;

namespace Knight.Prologue.Traversal;

[Flags]
public enum TraversalAbility
{
    None = 0,
    Jump = 1,
    Thread = 2,
    Dash = 4
}
