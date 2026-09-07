using System.Collections.Generic;

namespace Knight.Prologue.Guidance;

public interface IPrologueGuidanceVisual
{
    void Present(PrologueGuidanceInstruction instruction,
        IReadOnlyList<PrologueGuidanceTargetView> targets);
    void Clear();
}
