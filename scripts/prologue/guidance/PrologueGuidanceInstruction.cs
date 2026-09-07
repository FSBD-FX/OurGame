using System;
using System.Collections.Generic;

namespace Knight.Prologue.Guidance;

public sealed record PrologueGuidanceInstruction(
    bool Visible,
    string StepText,
    string HintText,
    string HeadingText,
    string HintPrefix,
    IReadOnlyList<PrologueGuidanceTargetKey> Targets)
{
    public static readonly PrologueGuidanceInstruction Hidden =
        new(false, string.Empty, string.Empty, string.Empty, string.Empty,
            Array.Empty<PrologueGuidanceTargetKey>());
}
